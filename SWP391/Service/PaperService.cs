using SWP391.Entities;
using SWP391.Models;
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

        public async Task<ServiceResult<object>> SearchPapersAsync(string? keyword, string? author, string? journal, int page, int pageSize)
        {
            var (papers, totalCount) = await _paperRepository.SearchPapersAsync(keyword, author, journal, page, pageSize);

            var result = new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                Items = papers.Select(p => new
                {
                    p.PaperId,
                    p.Title,
                    p.PublicationYear,
                    Journal = p.Journal?.JournalName,
                    Authors = p.Authors.Select(a => a.AuthorName).ToList(),
                    Keywords = p.Keywords.Select(k => k.KeywordText).ToList()
                })
            };

            return ServiceResult<object>.Ok(result);
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
                Journal = paper.Journal?.JournalName,
                Authors = paper.Authors.Select(a => a.AuthorName).ToList(),
                Keywords = paper.Keywords.Select(k => k.KeywordText).ToList()
            };

            return ServiceResult<object>.Ok(result);
        }
    }
}