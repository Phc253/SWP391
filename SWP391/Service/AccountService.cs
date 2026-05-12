using System.Net.Mail;
using System.Security.Cryptography;
using SWP391.Entities;
using SWP391.Models;
using SWP391.Models.Account;
using SWP391.Repositories;

namespace SWP391.Service
{
    public class AccountServices
    {
        private const int MinPasswordLength = 6;
        private const int MaxPasswordLength = 100;

        private readonly AccountRepository _accountRepository;

        public AccountServices(AccountRepository accountRepository)
        {
            _accountRepository = accountRepository;
        }

        public async Task<ServiceResult<RegisterResponse>> RegisterAsync(RegisterRequest request)
        {
            if (request == null)
            {
                return ServiceResult<RegisterResponse>.Fail("Request body is required.");
            }

            var email = request.Email?.Trim();
            var password = request.Password ?? string.Empty;
            var fullName = string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim();

            if (string.IsNullOrWhiteSpace(email))
            {
                return ServiceResult<RegisterResponse>.Fail("Email is required.");
            }

            if (!IsValidEmail(email))
            {
                return ServiceResult<RegisterResponse>.Fail("Email is invalid.");
            }

            if (password.Length < MinPasswordLength)
            {
                return ServiceResult<RegisterResponse>.Fail("Password must be at least 6 characters.");
            }

            if (password.Length > MaxPasswordLength)
            {
                return ServiceResult<RegisterResponse>.Fail("Password is too long.");
            }

            if (fullName != null && fullName.Length > 150)
            {
                return ServiceResult<RegisterResponse>.Fail("Full name is too long.");
            }

            var existingUser = await _accountRepository.GetUserByEmailAsync(email);
            if (existingUser != null)
            {
                return ServiceResult<RegisterResponse>.Fail("Email already exists.");
            }

            var user = new User
            {
                Email = email,
                PasswordHash = HashPassword(password),
                FullName = fullName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            var createdUser = await _accountRepository.CreateUserAsync(user);
            var response = new RegisterResponse
            {
                UserId = createdUser.UserId,
                Email = createdUser.Email,
                FullName = createdUser.FullName,
                CreatedAt = createdUser.CreatedAt,
                IsActive = createdUser.IsActive ?? true
            };

            return ServiceResult<RegisterResponse>.Ok(response);
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var address = new MailAddress(email);
                return address.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private static string HashPassword(string password)
        {
            var salt = new byte[16];
            RandomNumberGenerator.Fill(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256);
            var hash = pbkdf2.GetBytes(32);

            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }
    }
}
