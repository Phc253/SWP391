using Microsoft.EntityFrameworkCore;
using SWP391.Entities;

namespace SWP391.Repositories
{
    public class DashboardReportRepository
    {
        private readonly ScientificTrendDbContext _dbContext;

        public DashboardReportRepository(ScientificTrendDbContext dbContext)
            => _dbContext = dbContext;

        public async Task<DashboardReport> CreateAsync(DashboardReport report)
        {
            _dbContext.DashboardReports.Add(report);
            await _dbContext.SaveChangesAsync();
            return report;
        }

        public async Task<DashboardReport?> GetByIdAsync(long reportId)
            => await _dbContext.DashboardReports
                   .AsNoTracking()
                   .FirstOrDefaultAsync(r => r.ReportId == reportId);

        public async Task<(List<DashboardReport> Items, int TotalCount)> GetByUserIdAsync(
            int userId, int page, int pageSize)
        {
            var query = _dbContext.DashboardReports
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.GeneratedAt)
                .AsQueryable();

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync();

            return (items, total);
        }

        public async Task<DashboardReport?> UpdateAsync(long reportId, string name,
            string type, string? filterConfig)
        {
            var existing = await _dbContext.DashboardReports
                               .FirstOrDefaultAsync(r => r.ReportId == reportId);
            if (existing == null) return null;

            existing.ReportName   = name;
            existing.ReportType   = type;
            existing.FilterConfig = filterConfig;
            existing.GeneratedAt  = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();
            return existing;
        }

        public async Task<bool> DeleteAsync(long reportId)
        {
            var report = await _dbContext.DashboardReports
                             .FirstOrDefaultAsync(r => r.ReportId == reportId);
            if (report == null) return false;

            _dbContext.DashboardReports.Remove(report);
            await _dbContext.SaveChangesAsync();
            return true;
        }
    }
}
