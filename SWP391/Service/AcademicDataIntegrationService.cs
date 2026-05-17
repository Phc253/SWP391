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
                // Uri.EscapeDataString để đảm bảo các ký tự đặc biệt trong Keyword (nếu có khoảng trắng, @, &) không làm hỏng URL
                var encodedKeyword = Uri.EscapeDataString(keyword);
                // Call URL tìm kiếm các bài viết (works) dựa trên query 
                var url = $"https://api.openalex.org/works?search={encodedKeyword}&per-page={maxResults}";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"OpenAlex API failed with status code {response.StatusCode}");
                    return 0;
                }

                // Parse Json Data
                var content = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<OpenAlexResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (data?.Results == null || !data.Results.Any())
                {
                    return 0;
                }

                // 1. Kiểm tra / Tạo nguồn lấy dữ liệu trong DB giả định (ApiDataSource)
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
                    // 2. Lọc cơ bản: Bỏ qua những bài rác không có tiêu đề
                    if (string.IsNullOrWhiteSpace(work.Title)) continue;

                    // 3. Chống trùng lặp (duplication): Kiểm tra xem hệ thống đã lưu bài này (dựa trên ExternalId) chưa
                    var existingPaper = await _dbContext.Papers.FirstOrDefaultAsync(p => p.ExternalId == work.Id);
                    if (existingPaper != null) continue;

                    // Tạo đối tượng Paper mới
                    var paper = new Paper
                    {
                        Title = work.Title,
                        Abstract = BuildAbstract(work.AbstractInvertedIndex), // Parse chuỗi abstract
                        PublicationYear = work.PublicationYear,
                        ExternalId = work.Id,
                        SourceId = source.SourceId,
                        CreatedAt = DateTime.UtcNow
                    };

                    // 4. Handle Journal: Lưu thông tin Journal/Tạp chí nếu bài có chứa thông tin Journal
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

        // Phương thức này có tác dụng giải mã abstract_inverted_index trả về từ API OpenAlex.
        // API OpenAlex không trả về chuỗi văn bản thông thường cho Abstract mà dùng Index để tiết kiệm dung lượng.
        // Ex: {"keyword": [0,5], "technology": [1]} sẽ dịch lại thành thứ tự các từ dưa trên index.
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