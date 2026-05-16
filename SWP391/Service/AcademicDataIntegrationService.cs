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
            // Setting a User-Agent is often required/recommended for polite usage of APIs like OpenAlex
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ScientificTrendTracker/1.0 (mailto:admin@example.com)");
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<int> FetchAndSaveDataFromOpenAlexAsync(string keyword = "Computer Science", int maxResults = 50)
        {
            try
            {
                var encodedKeyword = Uri.EscapeDataString(keyword);
                var url = $"https://api.openalex.org/works?search={encodedKeyword}&per-page={maxResults}";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"OpenAlex API failed with status code {response.StatusCode}");
                    return 0;
                }

                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<OpenAlexResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (data?.Results == null || !data.Results.Any())
                {
                    return 0;
                }

                // Ensure OpenAlex ApiDataSource exists
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

                int savedCount = 0;
                foreach (var work in data.Results)
                {
                    // Basic validation
                    if (string.IsNullOrWhiteSpace(work.Title)) continue;

                    // Check if paper already exists
                    var existingPaper = await _dbContext.Papers.FirstOrDefaultAsync(p => p.ExternalId == work.Id);
                    if (existingPaper != null) continue;

                    var paper = new Paper
                    {
                        Title = work.Title,
                        Abstract = BuildAbstract(work.AbstractInvertedIndex),
                        PublicationYear = work.PublicationYear,
                        ExternalId = work.Id,
                        SourceId = source.SourceId,
                        CreatedAt = DateTime.UtcNow
                    };

                    // Handle Journal
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
                            await _dbContext.SaveChangesAsync(); // Save to get JournalId
                        }
                        paper.JournalId = journal.JournalId;
                    }

                    _dbContext.Papers.Add(paper);
                    await _dbContext.SaveChangesAsync(); // Save early to get PaperId for relationships

                    // Handle Authors
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
                                    await _dbContext.SaveChangesAsync(); // Save to get AuthorId
                                }
                                
                                paper.Authors.Add(author);
                            }
                        }
                    }

                    // Handle Keywords/Concepts
                    if (work.Concepts != null)
                    {
                        foreach (var concept in work.Concepts.Take(5)) // Take top 5 keywords based on relevance score if sorted
                        {
                            if (!string.IsNullOrWhiteSpace(concept.DisplayName))
                            {
                                var keywordEntity = await _dbContext.Keywords.FirstOrDefaultAsync(k => k.KeywordText == concept.DisplayName);
                                if (keywordEntity == null)
                                {
                                    keywordEntity = new Keyword { KeywordText = concept.DisplayName };
                                    _dbContext.Keywords.Add(keywordEntity);
                                    await _dbContext.SaveChangesAsync();
                                }
                                paper.Keywords.Add(keywordEntity);
                            }
                        }
                    }

                    await _dbContext.SaveChangesAsync();
                    savedCount++;
                }

                return savedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching and saving data from OpenAlex");
                throw;
            }
        }

        private string BuildAbstract(Dictionary<string, List<int>>? invertedIndex)
        {
            if (invertedIndex == null || !invertedIndex.Any()) return string.Empty;

            var words = new (string Word, int Index)[invertedIndex.Values.SelectMany(v => v).Max() + 1];
            
            foreach (var kvp in invertedIndex)
            {
                foreach (var index in kvp.Value)
                {
                    if (index < words.Length)
                    {
                        words[index] = (kvp.Key, index);
                    }
                }
            }

            return string.Join(" ", words.Where(w => w.Word != null).Select(w => w.Word));
        }
    }
}