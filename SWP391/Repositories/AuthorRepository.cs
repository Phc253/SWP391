using Microsoft.EntityFrameworkCore;
using SWP391.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SWP391.Repositories
{
    public class AuthorRepository
    {
        private readonly ScientificTrendDbContext _dbContext;

        public AuthorRepository(ScientificTrendDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Author?> GetAuthorByIdAsync(int authorId)
        {
            return await _dbContext.Authors
                .Include(a => a.Papers)
                    .ThenInclude(p => p.Journal)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AuthorId == authorId);
        }

        // Lấy danh sách tác giả theo danh sách IDs kèm theo Papers
        public async Task<List<Author>> GetAuthorsByIdsAsync(IEnumerable<int> ids)
        {
            return await _dbContext.Authors
                .Include(a => a.Papers)
                .Where(a => ids.Contains(a.AuthorId))
                .AsNoTracking()
                .ToListAsync();
        }

        // Tìm kiếm tác giả theo tên trong DB local
        public async Task<List<Author>> GetAuthorsByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return new List<Author>();
            }

            return await _dbContext.Authors
                .Where(a => a.AuthorName.Contains(name))
                .OrderBy(a => a.AuthorName)
                .AsNoTracking()
                .ToListAsync();
        }
    }
}
