using SWP391.Models;
using SWP391.Models.Papers;
using SWP391.Repositories;

namespace SWP391.Service
{
    public class PaperService
    {
        private readonly PaperRepository _paperRepository;

        public PaperService(PaperRepository paperRepository)
        {
            _paperRepository = paperRepository;
        }

        public Task<ServiceResult<object>> SearchPapersAsync(string? keyword, string? author, string? journal, int page, int pageSize)
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

        public async Task<ServiceResult<object>> SearchPapersAsync(PaperSearchRequest request)
        {
            NormalizePaging(request);

            var (papers, totalCount) = await _paperRepository.SearchPapersAsync(request);

            var result = new
            {
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize,
                Items = papers.Select(p => new
                {
                    p.PaperId,
                    p.Title,
                    p.PublicationYear,
                    p.CitationCount,
                    Journal = p.Journal?.JournalName,
                    Authors = p.Authors.Select(a => a.AuthorName).ToList(),
                    Keywords = p.Keywords.Select(k => k.KeywordText).ToList()
                })
            };

            return ServiceResult<object>.Ok(result);
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
    }
}
