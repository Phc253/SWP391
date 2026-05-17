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
            // Bước 1: Khởi tạo IQueryable cơ bản và nạp sẵn các liên kết (Eager Loading) cần thiết 
            // để lấy Journal, Authors, Keywords tránh lỗi N+1
            var query = _dbContext.Papers
                .Include(p => p.Journal)
                .Include(p => p.Authors)
                .Include(p => p.Keywords)
                .AsQueryable();

            // Bước 2: Thêm lọc theo Keyword (tìm trong Tiêu đề hoặc danh sách Keyword của bài)
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p => p.Title.Contains(keyword) || p.Keywords.Any(k => k.KeywordText.Contains(keyword)));
            }

            // Bước 3: Thêm lọc theo Tác giả (tìm bài có bất kỳ tác giả nào chứa chuỗi tìm kiếm)
            if (!string.IsNullOrWhiteSpace(author))
            {
                query = query.Where(p => p.Authors.Any(a => a.AuthorName.Contains(author)));
            }

            // Bước 4: Thêm lọc theo Tên Journal
            if (!string.IsNullOrWhiteSpace(journal))
            {
                query = query.Where(p => p.Journal != null && p.Journal.JournalName.Contains(journal));
            }

            // Lấy tổng số lượng bản ghi thỏa mãn điều kiện để support API phân trang (Pagination)
            int totalCount = await query.CountAsync();

            // Bước 5: Thực hiện phân trang và truy vấn Db
            // AsNoTracking() giúp lấy dữ liệu đọc (Read-only) nhanh hơn vì EF k cần theo dõi thay đổi
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
            // Lấy 1 bản ghi bằng ID kèm theo đầy đủ Journal, Authors, Keywords
            return await _dbContext.Papers
                .Include(p => p.Journal)
                .Include(p => p.Authors)
                .Include(p => p.Keywords)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PaperId == id);
        }
    }
}