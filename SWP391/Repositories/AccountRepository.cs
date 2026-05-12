using Microsoft.EntityFrameworkCore;
using SWP391.Entities;

namespace SWP391.Repositories
{
    public class AccountRepository
    {
        private readonly ScientificTrendDbContext _dbContext;

        public AccountRepository(ScientificTrendDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<User?> GetUserByEmailAsync(string email)
        {
            return _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> CreateUserAsync(User user)
        {
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
            return user;
        }
    }
}
