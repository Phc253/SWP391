using Microsoft.Extensions.Logging;
using SWP391.Models;
using SWP391.Models.Author;
using SWP391.Models.Integration;
using SWP391.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace SWP391.Service
{
    public class AuthorService
    {
        private readonly AuthorRepository _authorRepository;
        private readonly FollowRepository _followRepository;
        private readonly HttpClient _httpClient;
        private readonly UserQuotaService _userQuotaService;
        private readonly ILogger<AuthorService> _logger;

        public AuthorService(
            AuthorRepository authorRepository,
            FollowRepository followRepository,
            HttpClient httpClient,
            UserQuotaService userQuotaService,
            ILogger<AuthorService> logger)
        {
            _authorRepository = authorRepository;
            _followRepository = followRepository;
            _httpClient = httpClient;
            _userQuotaService = userQuotaService;
            // Setting a User-Agent is recommended for polite usage of APIs like OpenAlex
            if (!_httpClient.DefaultRequestHeaders.Contains("User-Agent"))
            {
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "ScientificTrendTracker/1.0 (mailto:admin@example.com)");
            }
            _logger = logger;
        }

        public async Task<ServiceResult<AuthorProfileResponse>> GetAuthorProfileAsync(int authorId, int? userId = null)
        {
            // 1. Lấy thông tin từ database local
            var author = await _authorRepository.GetAuthorByIdAsync(authorId);
            if (author == null)
            {
                return ServiceResult<AuthorProfileResponse>.Fail("Author not found.");
            }

            // 2. Map dữ liệu cơ bản từ local DB
            var profile = new AuthorProfileResponse
            {
                AuthorId = author.AuthorId,
                AuthorName = author.AuthorName,
                Papers = author.Papers.Select(p => new AuthorPaperItem
                {
                    PaperId = p.PaperId,
                    Title = p.Title,
                    PublicationYear = p.PublicationYear,
                    CitationCount = p.CitationCount,
                    JournalName = p.Journal?.JournalName
                }).OrderByDescending(p => p.PublicationYear).ToList()
            };

            // 3. Kiểm tra trạng thái follow nếu user đã đăng nhập
            if (userId.HasValue)
            {
                profile.IsFollowed = await _followRepository.IsFollowingAsync(userId.Value, authorId, "Author");
            }

            // 4. Bổ sung thông tin (Enrichment) từ OpenAlex API
            try
            {
                var budgetCheck = await _userQuotaService.CheckAndConsumeBudgetAsync(userId, "Budget:Cost:EnrichAuthor");
                if (!budgetCheck.Success)
                {
                    _logger.LogWarning("Skipping Author profile enrichment for '{AuthorName}' because: {Reason}", author.AuthorName, budgetCheck.Error);
                    return ServiceResult<AuthorProfileResponse>.Ok(profile);
                }

                var encodedName = Uri.EscapeDataString(author.AuthorName);
                var url = $"https://api.openalex.org/authors?search={encodedName}";

                _logger.LogInformation("Calling OpenAlex Author API: {Url}", url);
                var response = await _httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var searchResponse = JsonSerializer.Deserialize<OpenAlexAuthorSearchResponse>(content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    var matchedAuthor = searchResponse?.Results?.FirstOrDefault(a => 
                        string.Equals(a.DisplayName, author.AuthorName, StringComparison.OrdinalIgnoreCase));
                    
                    // Nếu không có khớp chính xác, lấy kết quả đầu tiên nếu có
                    if (matchedAuthor == null && searchResponse?.Results != null && searchResponse.Results.Any())
                    {
                        matchedAuthor = searchResponse.Results.First();
                    }

                    if (matchedAuthor != null)
                    {
                        profile.WorksCount = matchedAuthor.WorksCount;
                        profile.CitedByCount = matchedAuthor.CitedByCount;

                        if (matchedAuthor.LastKnownInstitutions != null && matchedAuthor.LastKnownInstitutions.Any())
                        {
                            profile.Affiliation = string.Join(", ", matchedAuthor.LastKnownInstitutions
                                .Where(inst => !string.IsNullOrEmpty(inst.DisplayName))
                                .Select(inst => inst.DisplayName));
                        }
                        
                        _logger.LogInformation("Enriched Author {AuthorName} successfully. Affiliation: {Affiliation}", 
                            author.AuthorName, profile.Affiliation);
                    }
                }
                else
                {
                    _logger.LogWarning("OpenAlex Author API failed with status {StatusCode} for name {AuthorName}", 
                        response.StatusCode, author.AuthorName);
                }
            }
            catch (Exception ex)
            {
                // Thất bại khi gọi API OpenAlex không nên làm sập cả request lấy Profile của User, 
                // chúng ta chỉ cần log lỗi và trả về profile từ DB local.
                _logger.LogError(ex, "Error enriching author profile from OpenAlex for {AuthorName}", author.AuthorName);
            }

            return ServiceResult<AuthorProfileResponse>.Ok(profile);
        }

        public async Task<ServiceResult<List<AuthorProfileResponse>>> SearchAuthorsAsync(string name)
        {
            var authors = await _authorRepository.GetAuthorsByNameAsync(name);
            var result = authors.Select(a => new AuthorProfileResponse
            {
                AuthorId = a.AuthorId,
                AuthorName = a.AuthorName
            }).ToList();

            return ServiceResult<List<AuthorProfileResponse>>.Ok(result);
        }
    }
}
