using SWP391.Models;
using SWP391.Models.Papers;
using SWP391.Repositories;
using SWP391.Entities;

namespace SWP391.Service
{
    public class PaperService
    {
        private readonly PaperRepository _paperRepository;

        public PaperService(PaperRepository paperRepository)
        {
            _paperRepository = paperRepository;
        }

        public Task<ServiceResult<PaperListResponse>> SearchPapersAsync(string? keyword, string? author, string? journal, int page, int pageSize)
        {
            return SearchPapersAsync(new PaperSearchRequest
            {
                Keyword = keyword,
                Author = author,
                Journal = journal,
                Page = page,
                PageSize = pageSize
            });
        }

        public async Task<ServiceResult<PaperListResponse>> SearchPapersAsync(PaperSearchRequest request)
        {
            NormalizePaging(request);

            var (papers, totalCount) = await _paperRepository.SearchPapersAsync(request);

            var result = ToPaperListResponse(papers, totalCount, request.Page, request.PageSize);

            return ServiceResult<PaperListResponse>.Ok(result);
        }

        public async Task<ServiceResult<PaperFacetResponse>> GetAuthorFacetsAsync(string? q, int page, int pageSize)
        {
            NormalizePaging(ref page, ref pageSize);
            var response = await _paperRepository.GetAuthorFacetsAsync(q, page, pageSize);
            return ServiceResult<PaperFacetResponse>.Ok(response);
        }

        public async Task<ServiceResult<PaperFacetResponse>> GetKeywordFacetsAsync(string? q, int page, int pageSize)
        {
            NormalizePaging(ref page, ref pageSize);
            var response = await _paperRepository.GetKeywordFacetsAsync(q, page, pageSize);
            return ServiceResult<PaperFacetResponse>.Ok(response);
        }

        public async Task<ServiceResult<PaperFacetResponse>> GetTopicFacetsAsync(string? q, int page, int pageSize)
        {
            NormalizePaging(ref page, ref pageSize);
            var response = await _paperRepository.GetTopicFacetsAsync(q, page, pageSize);
            return ServiceResult<PaperFacetResponse>.Ok(response);
        }

        public async Task<ServiceResult<PaperFacetResponse>> GetJournalFacetsAsync(string? q, int page, int pageSize)
        {
            NormalizePaging(ref page, ref pageSize);
            var response = await _paperRepository.GetJournalFacetsAsync(q, page, pageSize);
            return ServiceResult<PaperFacetResponse>.Ok(response);
        }

        public async Task<ServiceResult<PaperListResponse>> GetPapersByTopicAsync(int topicId, int page, int pageSize)
        {
            if (topicId <= 0)
            {
                return ServiceResult<PaperListResponse>.Fail("Topic id must be greater than 0");
            }

            NormalizePaging(ref page, ref pageSize);

            var request = new PaperSearchRequest
            {
                TopicIds = new List<int> { topicId },
                Page = page,
                PageSize = pageSize
            };

            var (papers, totalCount) = await _paperRepository.SearchPapersAsync(request);
            var response = ToPaperListResponse(papers, totalCount, page, pageSize);

            return ServiceResult<PaperListResponse>.Ok(response);
        }

        public async Task<ServiceResult<PaperListResponse>> GetPapersByJournalAsync(int journalId, int page, int pageSize)
        {
            if (journalId <= 0)
            {
                return ServiceResult<PaperListResponse>.Fail("Journal id must be greater than 0");
            }

            NormalizePaging(ref page, ref pageSize);

            var request = new PaperSearchRequest
            {
                JournalIds = new List<int> { journalId },
                Page = page,
                PageSize = pageSize
            };

            var (papers, totalCount) = await _paperRepository.SearchPapersAsync(request);
            var response = ToPaperListResponse(papers, totalCount, page, pageSize);

            return ServiceResult<PaperListResponse>.Ok(response);
        }

        public async Task<ServiceResult<object>> GetPaperDetailsAsync(long id)
        {
            var paper = await _paperRepository.GetPaperByIdAsync(id);
            if (paper == null)
            {
                return ServiceResult<object>.Fail("Paper not found");
            }

            var result = new
            {
                paper.PaperId,
                paper.Title,
                paper.Abstract,
                paper.PublicationYear,
                paper.CitationCount,
                Journal = paper.Journal?.JournalName,
                Authors = paper.Authors.Select(a => a.AuthorName).ToList(),
                Keywords = paper.Keywords.Select(k => k.KeywordText).ToList()
            };

            return ServiceResult<object>.Ok(result);
        }

        private static void NormalizePaging(PaperSearchRequest request)
        {
            request.Page = Math.Max(1, request.Page);
            request.PageSize = Math.Clamp(request.PageSize, 1, 100);
        }

        private static void NormalizePaging(ref int page, ref int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);
        }

        private static PaperListResponse ToPaperListResponse(List<Paper> papers, int totalCount, int page, int pageSize)
        {
            return new PaperListResponse
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Items = papers.Select(p => new PaperListItemResponse
                {
                    PaperId = p.PaperId,
                    Title = p.Title,
                    PublicationYear = p.PublicationYear,
                    CitationCount = p.CitationCount,
                    Journal = p.Journal?.JournalName,
                    Authors = p.Authors
                        .Select(a => a.AuthorName ?? string.Empty)
                        .ToList(),
                    Keywords = p.Keywords
                        .Select(k => k.KeywordText ?? string.Empty)
                        .ToList()
                }).ToList()
            };
        }
    }
}
