using Microsoft.EntityFrameworkCore;
using SWP391.Entities;

namespace SWP391.Repositories
{
    public class PaperRepository
    {
        private readonly ScientificTrendDbContext _dbContext;

        public PaperRepository(ScientificTrendDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<(List<Paper> Papers, int TotalCount)> SearchPapersAsync(string? keyword, string? author, string? journal, int page, int pageSize)
        {
            var query = _dbContext.Papers
                .Include(p => p.Journal)
                .Include(p => p.Authors)
                .Include(p => p.Keywords)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p => p.Title.Contains(keyword) || p.Keywords.Any(k => k.KeywordText.Contains(keyword)));
            }

            if (!string.IsNullOrWhiteSpace(author))
            {
                query = query.Where(p => p.Authors.Any(a => a.AuthorName.Contains(author)));
            }

            if (!string.IsNullOrWhiteSpace(journal))
            {
                query = query.Where(p => p.Journal != null && p.Journal.JournalName.Contains(journal));
            }

            int totalCount = await query.CountAsync();

            var papers = await query
                .OrderByDescending(p => p.PublicationYear)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return (papers, totalCount);
        }

        public async Task<Paper?> GetPaperByIdAsync(long id)
        {
            return await _dbContext.Papers
                .Include(p => p.Journal)
                .Include(p => p.Authors)
                .Include(p => p.Keywords)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PaperId == id);
        }
    }
}