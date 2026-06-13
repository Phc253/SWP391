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
                var encodedKeyword = Uri.EscapeDataString(keyword);
                var url = $"https://api.openalex.org/works?search={encodedKeyword}&per-page={maxResults}";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OpenAlex API failed with status code {Status}", response.StatusCode);
                    return new DataIngestionResult();
                }

                _logger.LogInformation("OpenAlex API call succeeded. Url={Url} Status={Status}.", url, response.StatusCode);

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<OpenAlexResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (data?.Results == null || !data.Results.Any())
                {
                    _logger.LogInformation("OpenAlex returned no results for keyword {Keyword}.", keyword);
                    return new DataIngestionResult();
                }

                _logger.LogInformation("OpenAlex returned {Count} results for keyword {Keyword}.", data.Results.Count, keyword);

                var source = await EnsureOpenAlexSourceAsync();

                var newPaperIds = new List<long>();
                foreach (var work in data.Results)
                {
                    var paperId = await ProcessWorkAsync(work, source);
                    if (paperId.HasValue)
                    {
                        newPaperIds.Add(paperId.Value);
                    }
                }

                return new DataIngestionResult
                {
                    SavedCount = newPaperIds.Count,
                    NewPaperIds = newPaperIds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching and saving data from OpenAlex");
                throw;
            }
        }

        // Fetches papers from OpenAlex for multiple keywords with optional year-range filter.
        // URL pattern: /works?search={keyword}[&filter=publication_year:{yearFrom}-{yearTo}]&per-page={max}
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
                    var url = $"https://api.openalex.org/works?search={encodedKeyword}{filterClause}&per-page={maxResultsPerKeyword}";

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
                        if ((await ProcessWorkAsync(work, source)).HasValue)
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

        public async Task<int?> FetchCitationCountFromOpenAlexAsync(string externalId)
        {
            var workId = NormalizeOpenAlexWorkId(externalId);
            if (string.IsNullOrWhiteSpace(workId))
            {
                return null;
            }

            var url = $"https://api.openalex.org/works/{Uri.EscapeDataString(workId)}";
            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "OpenAlex citation fetch failed for ExternalId={ExternalId} Status={Status}",
                    externalId,
                    response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var work = JsonSerializer.Deserialize<WorkData>(
                content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return work?.CitationCount;
        }

        // Processes a single OpenAlex work: deduplicates, creates journal/authors/keywords, saves paper.
        // Returns the new PaperId when saved, or null for duplicates/invalid works.
        private async Task<long?> ProcessWorkAsync(WorkData work, ApiDataSource source)
        {
            if (string.IsNullOrWhiteSpace(work.Title)) return null;

            var existingPaper = await _dbContext.Papers.FirstOrDefaultAsync(p => p.ExternalId == work.Id);
            if (existingPaper != null)
            {
                _logger.LogDebug("Skipping existing paper ExternalId={ExternalId} Title={Title}", work.Id, work.Title);
                return null;
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
            return paper.PaperId;
        }

        private static string? NormalizeOpenAlexWorkId(string? externalId)
        {
            if (string.IsNullOrWhiteSpace(externalId))
            {
                return null;
            }

            var trimmed = externalId.Trim();
            var lastSlash = trimmed.LastIndexOf('/');
            return lastSlash >= 0 ? trimmed[(lastSlash + 1)..] : trimmed;
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
    }
}
