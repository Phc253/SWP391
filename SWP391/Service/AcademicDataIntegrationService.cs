using Microsoft.EntityFrameworkCore;
using SWP391.Entities;
using SWP391.Models.Integration;
using System.Text.Json;

namespace SWP391.Service
{
    public class AcademicDataIntegrationService
    {
        private readonly HttpClient _httpClient;
        private readonly ScientificTrendDbContext _dbContext;
        private readonly ILogger<AcademicDataIntegrationService> _logger;

        public AcademicDataIntegrationService(HttpClient httpClient, ScientificTrendDbContext dbContext, ILogger<AcademicDataIntegrationService> logger)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ScientificTrendTracker/1.0 (mailto:admin@example.com)");
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<DataIngestionResult> FetchAndSaveDataFromOpenAlexAsync(string keyword = "Computer Science", int maxResults = 40)
        {
            try
            {
                maxResults = Math.Clamp(maxResults, 1, 100);
                var encodedKeyword = Uri.EscapeDataString(keyword);
                var url = $"https://api.openalex.org/works?search={encodedKeyword}&per_page={maxResults}";

                return await FetchAndProcessOpenAlexListAsync(url, keyword, refreshExistingCitation: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching and saving data from OpenAlex");
                throw;
            }
        }

        public async Task<DataIngestionResult> FetchOpenAlexWorksAsync(
            string keyword = "Computer Science",
            int maxResults = 40,
            string? cursor = null)
        {
            try
            {
                maxResults = Math.Clamp(maxResults, 1, 100);
                var currentYear = DateTime.UtcNow.Year;
                var encodedKeyword = Uri.EscapeDataString(keyword);
                var cursorClause = string.IsNullOrWhiteSpace(cursor)
                    ? string.Empty
                    : $"&cursor={Uri.EscapeDataString(cursor)}";
                var url = $"https://api.openalex.org/works?search.title_and_abstract={encodedKeyword}&filter=publication_year:{currentYear}&sort=cited_by_count:desc&per_page={maxResults}{cursorClause}";

                return await FetchAndProcessOpenAlexListAsync(url, keyword, refreshExistingCitation: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching citation-ranked works from OpenAlex");
                throw;
            }
        }

        public async Task<DataIngestionResult> RefreshExistingOpenAlexWorksAsync(IEnumerable<string> externalIds)
        {
            var result = new DataIngestionResult();
            var source = await EnsureOpenAlexSourceAsync();

            foreach (var externalId in externalIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct())
            {
                try
                {
                    var workId = GetOpenAlexWorkIdForPath(externalId);
                    if (string.IsNullOrWhiteSpace(workId))
                    {
                        continue;
                    }

                    var url = $"https://api.openalex.org/works/{Uri.EscapeDataString(workId)}";
                    var response = await _httpClient.GetAsync(url);
                    var content = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning(
                            "OpenAlex refresh failed. Url={Url} ExternalId={ExternalId} Status={Status} Body={Body}",
                            url,
                            externalId,
                            response.StatusCode,
                            content);
                        continue;
                    }

                    var work = JsonSerializer.Deserialize<WorkData>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (work == null)
                    {
                        continue;
                    }

                    result.FetchedCount++;
                    var processed = await ProcessWorkAsync(work, source, refreshExistingCitation: true);
                    if (processed.NewPaperId.HasValue)
                    {
                        result.NewPaperIds.Add(processed.NewPaperId.Value);
                        result.SavedCount++;
                    }

                    if (processed.UpdatedExisting)
                    {
                        result.UpdatedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error refreshing OpenAlex work ExternalId={ExternalId}; skipping", externalId);
                }
            }

            return result;
        }

        // Fetches papers from OpenAlex for multiple keywords with optional year-range filter.
        // URL pattern: /works?search={keyword}[&filter=publication_year:{yearFrom}-{yearTo}]&per_page={max}
        // Per-keyword failures are logged and skipped — the batch continues.
        public async Task<int> FetchAndSaveFilteredAsync(
            IEnumerable<string> keywords,
            int? yearFrom = null,
            int? yearTo = null,
            int maxResultsPerKeyword = 40)
        {
            var source = await EnsureOpenAlexSourceAsync();
            int totalSaved = 0;

            foreach (var keyword in keywords)
            {
                try
                {
                    var encodedKeyword = Uri.EscapeDataString(keyword);
                    string filterClause = (yearFrom.HasValue && yearTo.HasValue)
                        ? $"&filter=publication_year:{yearFrom}-{yearTo}"
                        : string.Empty;
                    var url = $"https://api.openalex.org/works?search={encodedKeyword}{filterClause}&per_page={maxResultsPerKeyword}";

                    var response = await _httpClient.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("OpenAlex filter fetch failed for keyword '{Keyword}' status {Status}", keyword, response.StatusCode);
                        continue;
                    }

                    var content = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<OpenAlexResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (data?.Results == null) continue;

                    _logger.LogInformation("OpenAlex returned {Count} results for keyword '{Keyword}'.", data.Results.Count, keyword);

                    foreach (var work in data.Results)
                    {
                        if ((await ProcessWorkAsync(work, source, refreshExistingCitation: false)).NewPaperId.HasValue)
                            totalSaved++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error fetching keyword '{Keyword}' — skipping", keyword);
                }
            }

            return totalSaved;
        }

        private async Task<DataIngestionResult> FetchAndProcessOpenAlexListAsync(
            string url,
            string keyword,
            bool refreshExistingCitation)
        {
            var response = await _httpClient.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "OpenAlex API failed. Url={Url} Status={Status} Body={Body}",
                    url,
                    response.StatusCode,
                    content);
                return new DataIngestionResult();
            }

            _logger.LogInformation("OpenAlex API call succeeded. Url={Url} Status={Status}.", url, response.StatusCode);

            var data = JsonSerializer.Deserialize<OpenAlexResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (data?.Results == null || !data.Results.Any())
            {
                _logger.LogInformation("OpenAlex returned no results for keyword {Keyword}.", keyword);
                return new DataIngestionResult();
            }

            _logger.LogInformation("OpenAlex returned {Count} results for keyword {Keyword}.", data.Results.Count, keyword);

            var source = await EnsureOpenAlexSourceAsync();
            var result = new DataIngestionResult
            {
                FetchedCount = data.Results.Count,
                NextCursor = data.Meta?.NextCursor
            };

            foreach (var work in data.Results)
            {
                var processed = await ProcessWorkAsync(work, source, refreshExistingCitation);
                if (processed.NewPaperId.HasValue)
                {
                    result.NewPaperIds.Add(processed.NewPaperId.Value);
                    result.SavedCount++;
                }

                if (processed.UpdatedExisting)
                {
                    result.UpdatedCount++;
                }
            }

            return result;
        }

        // Ensures the OpenAlex ApiDataSource row exists and returns it.
        private async Task<ApiDataSource> EnsureOpenAlexSourceAsync()
        {
            var source = await _dbContext.ApiDataSources.FirstOrDefaultAsync(s => s.SourceName == "OpenAlex");
            if (source == null)
            {
                source = new ApiDataSource
                {
                    SourceName = "OpenAlex",
                    BaseUrl = "https://api.openalex.org",
                    IsActive = true
                };
                _dbContext.ApiDataSources.Add(source);
                await _dbContext.SaveChangesAsync();
            }
            return source;
        }

        // Processes a single OpenAlex work: deduplicates, creates journal/authors/keywords, saves paper.
        private async Task<ProcessedWorkResult> ProcessWorkAsync(
            WorkData work,
            ApiDataSource source,
            bool refreshExistingCitation)
        {
            if (string.IsNullOrWhiteSpace(work.Title))
                return new ProcessedWorkResult();

            var existingPaper = await _dbContext.Papers.FirstOrDefaultAsync(p => p.ExternalId == work.Id);
            if (existingPaper != null)
            {
                if (refreshExistingCitation &&
                    work.CitationCount is int citationCount &&
                    existingPaper.CitationCount != citationCount)
                {
                    existingPaper.CitationCount = citationCount;
                    await _dbContext.SaveChangesAsync();

                    _logger.LogDebug("Updated citation count for existing paper ExternalId={ExternalId} Title={Title}", work.Id, work.Title);
                    return new ProcessedWorkResult { UpdatedExisting = true };
                }

                _logger.LogDebug("Skipping existing paper ExternalId={ExternalId} Title={Title}", work.Id, work.Title);
                return new ProcessedWorkResult();
            }

            var paper = new Paper
            {
                Title = work.Title,
                Abstract = BuildAbstract(work.AbstractInvertedIndex),
                PublicationYear = work.PublicationYear,
                CitationCount = work.CitationCount,
                ExternalId = work.Id,
                SourceId = source.SourceId,
                CreatedAt = DateTime.UtcNow
            };

            var sourceData = work.PrimaryLocation?.Source;
            if (sourceData != null && !string.IsNullOrWhiteSpace(sourceData.DisplayName))
            {
                var journal = await _dbContext.Journals.FirstOrDefaultAsync(j => j.JournalName == sourceData.DisplayName);
                if (journal == null)
                {
                    journal = new Journal
                    {
                        JournalName = sourceData.DisplayName,
                        Issn = sourceData.Issn,
                        Publisher = sourceData.Publisher
                    };
                    _dbContext.Journals.Add(journal);
                    await _dbContext.SaveChangesAsync();
                }
                paper.JournalId = journal.JournalId;
            }

            _dbContext.Papers.Add(paper);
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Saved Paper Id={PaperId} ExternalId={ExternalId} Title={Title}", paper.PaperId, paper.ExternalId, paper.Title);

            if (work.Authorships != null)
            {
                foreach (var authorship in work.Authorships)
                {
                    var authorData = authorship.Author;
                    if (authorData != null && !string.IsNullOrWhiteSpace(authorData.DisplayName))
                    {
                        var author = await _dbContext.Authors.FirstOrDefaultAsync(a => a.AuthorName == authorData.DisplayName);
                        if (author == null)
                        {
                            author = new Author { AuthorName = authorData.DisplayName };
                            _dbContext.Authors.Add(author);
                            await _dbContext.SaveChangesAsync();
                        }
                        paper.Authors.Add(author);
                    }
                }
            }

            if (work.Concepts != null && work.Concepts.Any())
            {
                var sortedConcepts = work.Concepts.OrderByDescending(c => c.Score).ToList();

                var topicConcept = sortedConcepts.FirstOrDefault(c => c.Level <= 1 && !string.IsNullOrWhiteSpace(c.DisplayName));
                ResearchTopic? currentTopic = null;

                if (topicConcept != null)
                {
                    currentTopic = await _dbContext.ResearchTopics.FirstOrDefaultAsync(t => t.TopicName == topicConcept.DisplayName);
                    if (currentTopic == null)
                    {
                        currentTopic = new ResearchTopic { TopicName = topicConcept.DisplayName };
                        _dbContext.ResearchTopics.Add(currentTopic);
                        await _dbContext.SaveChangesAsync();
                    }
                }

                var keywordConcepts = sortedConcepts.Where(c => c.Level >= 2 && !string.IsNullOrWhiteSpace(c.DisplayName)).Take(5);
                foreach (var concept in keywordConcepts)
                {
                    var keywordEntity = await _dbContext.Keywords.FirstOrDefaultAsync(k => k.KeywordText == concept.DisplayName);
                    if (keywordEntity == null)
                    {
                        keywordEntity = new Keyword
                        {
                            KeywordText = concept.DisplayName,
                            TopicId = currentTopic?.TopicId
                        };
                        _dbContext.Keywords.Add(keywordEntity);
                        await _dbContext.SaveChangesAsync();
                    }
                    else if (keywordEntity.TopicId == null && currentTopic != null)
                    {
                        keywordEntity.TopicId = currentTopic.TopicId;
                        await _dbContext.SaveChangesAsync();
                    }

                    paper.Keywords.Add(keywordEntity);
                }
            }

            await _dbContext.SaveChangesAsync();
            return new ProcessedWorkResult { NewPaperId = paper.PaperId };
        }

        private static string GetOpenAlexWorkIdForPath(string externalId)
        {
            var trimmed = externalId.Trim();
            if (Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            {
                return uri.Segments.LastOrDefault()?.Trim('/') ?? string.Empty;
            }

            return trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? trimmed;
        }

        // Decodes OpenAlex abstract_inverted_index (word → position list) back into plain text.
        private string BuildAbstract(Dictionary<string, List<int>>? invertedIndex)
        {
            if (invertedIndex == null || !invertedIndex.Any()) return string.Empty;

            var words = new (string Word, int Index)[invertedIndex.Values.SelectMany(v => v).Max() + 1];

            foreach (var kvp in invertedIndex)
            {
                foreach (var index in kvp.Value)
                {
                    if (index < words.Length)
                        words[index] = (kvp.Key, index);
                }
            }

            return string.Join(" ", words.Where(w => w.Word != null).Select(w => w.Word));
        }

        private class ProcessedWorkResult
        {
            public long? NewPaperId { get; set; }
            public bool UpdatedExisting { get; set; }
        }
    }
}
