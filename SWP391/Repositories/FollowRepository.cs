using Microsoft.EntityFrameworkCore;
using SWP391.Entities;

namespace SWP391.Repositories
{
    public class FollowRepository
    {
        private readonly ScientificTrendDbContext _dbContext;

        public FollowRepository(ScientificTrendDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Follow?> GetFollowAsync(int userId, long targetId, string targetType)
        {
            var targetTypes = GetEquivalentTargetTypes(targetType);

            return await _dbContext.Follows
                .FirstOrDefaultAsync(f =>
                    f.UserId == userId &&
                    f.TargetId == targetId &&
                    targetTypes.Contains(f.TargetType.ToLower()));
        }

        public async Task<bool> IsFollowingAsync(int userId, long targetId, string targetType)
        {
            var targetTypes = GetEquivalentTargetTypes(targetType);

            return await _dbContext.Follows
                .AnyAsync(f =>
                    f.UserId == userId &&
                    f.TargetId == targetId &&
                    targetTypes.Contains(f.TargetType.ToLower()));
        }

        public async Task<List<Follow>> GetFollowsByTargetIdsAsync(string targetType, IEnumerable<long> targetIds)
        {
            return await GetFollowsByTargetTypesAsync(new[] { targetType }, targetIds);
        }

        public async Task<List<Follow>> GetFollowsByTargetTypesAsync(IEnumerable<string> targetTypes, IEnumerable<long> targetIds)
        {
            var ids = targetIds.Distinct().ToList();
            if (!ids.Any())
            {
                return new List<Follow>();
            }

            var normalizedTypes = targetTypes
                .SelectMany(GetEquivalentTargetTypes)
                .Distinct()
                .ToList();

            if (!normalizedTypes.Any())
            {
                return new List<Follow>();
            }

            return await _dbContext.Follows
                .Where(f => ids.Contains(f.TargetId) && normalizedTypes.Contains(f.TargetType.ToLower()))
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<List<Follow>> GetUserFollowsAsync(int userId)
        {
            return await _dbContext.Follows
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task AddFollowAsync(Follow follow)
        {
            await _dbContext.Follows.AddAsync(follow);
            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveFollowAsync(Follow follow)
        {
            _dbContext.Follows.Remove(follow);
            await _dbContext.SaveChangesAsync();
        }

        private static List<string> GetEquivalentTargetTypes(string targetType)
        {
            var normalized = targetType.Trim().ToLower();
            if (normalized == "topic" || normalized == "researchtopic")
            {
                return new List<string> { "topic", "researchtopic" };
            }

            return new List<string> { normalized };
        }
    }
}
