using System.Net.Mail;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SWP391.Entities;
using SWP391.Models;
using SWP391.Models.Account;
using SWP391.Repositories;

namespace SWP391.Service
{
    public class AccountService
    {
        private const int MinPasswordLength = 6;
        private const int MaxPasswordLength = 100;

        private readonly AccountRepository _accountRepository;
        private readonly IConfiguration _configuration;

        public AccountService(AccountRepository accountRepository, IConfiguration configuration)
        {
            _accountRepository = accountRepository;
            _configuration = configuration;
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

        public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                return ServiceResult<LoginResponse>.Fail("Invalid credentials.");
            }

            var user = await _accountRepository.GetUserByEmailAsync(request.Email);
            if (user == null)
            {
                return ServiceResult<LoginResponse>.Fail("User not found.");
            }

            if (!VerifyPassword(request.Password, user.PasswordHash))
            {
                return ServiceResult<LoginResponse>.Fail("Invalid password.");
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["JWT:Secret"] ?? "SuperSecretKeyForJWTWhichMustBeMoreThan16Chars!");
            
            // [CẬP NHẬT] Tạo danh sách Claims chứa thông tin cơ bản
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName ?? string.Empty)
            };

            // [CẬP NHẬT] Lặp qua các Role của User (đã được Include từ Repository) và đẩy vào Token
            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role.RoleName));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = _configuration["JWT:ValidIssuer"],
                Audience = _configuration["JWT:ValidAudience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return ServiceResult<LoginResponse>.Ok(new LoginResponse 
            { 
                Token = tokenString,
                Email = user.Email
            });
        }

        private static bool VerifyPassword(string enteredPassword, string storedHash)
        {
            var parts = storedHash.Split('.', 2);
            if (parts.Length != 2)
            {
                return false;
            }

            var salt = Convert.FromBase64String(parts[0]);
            var hash = Convert.FromBase64String(parts[1]);

            using var pbkdf2 = new Rfc2898DeriveBytes(enteredPassword, salt, 10000, HashAlgorithmName.SHA256);
            var testHash = pbkdf2.GetBytes(32);

            return CryptographicOperations.FixedTimeEquals(hash, testHash);
        }
    }
}
