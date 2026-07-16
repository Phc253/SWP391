using System.Net.Mail;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SWP391.Entities;
using SWP391.Models;
using SWP391.Models.Account;
using SWP391.Models.Common;
using SWP391.Repositories;

namespace SWP391.Service
{
    public class AccountService
    {
        private const int MinPasswordLength = 6;
        private const int MaxPasswordLength = 100;
        private const int EmailVerificationTokenHours = 24;
        private const string InvalidCredentialsMessage = "Invalid email or password.";

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
                return ServiceResult<RegisterResponse>.Fail("Request body is required.", ErrorCodes.ValidationError);
            }

            var email = request.Email?.Trim();
            var password = request.Password ?? string.Empty;
            var fullName = string.IsNullOrWhiteSpace(request.FullName) ? null : request.FullName.Trim();
            var phoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();
            var actorType = UserActorTypes.Normalize(request.ActorType);

            if (string.IsNullOrWhiteSpace(email))
            {
                return ServiceResult<RegisterResponse>.Fail("Email is required.", ErrorCodes.ValidationError);
            }

            if (!IsValidEmail(email))
            {
                return ServiceResult<RegisterResponse>.Fail("Email is invalid.", ErrorCodes.ValidationError);
            }

            if (password.Length < MinPasswordLength)
            {
                return ServiceResult<RegisterResponse>.Fail("Password must be at least 6 characters.", ErrorCodes.ValidationError);
            }

            if (password.Length > MaxPasswordLength)
            {
                return ServiceResult<RegisterResponse>.Fail("Password is too long.", ErrorCodes.ValidationError);
            }

            if (fullName != null && fullName.Length > 150)
            {
                return ServiceResult<RegisterResponse>.Fail("Full name is too long.", ErrorCodes.ValidationError);
            }

            if (!request.DateOfBirth.HasValue)
            {
                return ServiceResult<RegisterResponse>.Fail("Date of birth is required.", ErrorCodes.ValidationError);
            }

            if (request.DateOfBirth.Value.Date > DateTime.UtcNow.Date)
            {
                return ServiceResult<RegisterResponse>.Fail("Date of birth cannot be in the future.", ErrorCodes.ValidationError);
            }

            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return ServiceResult<RegisterResponse>.Fail("Phone number is required.", ErrorCodes.ValidationError);
            }

            if (phoneNumber.Length > 20)
            {
                return ServiceResult<RegisterResponse>.Fail("Phone number is too long.", ErrorCodes.ValidationError);
            }

            if (string.IsNullOrEmpty(actorType))
            {
                return ServiceResult<RegisterResponse>.Fail("ActorType must be one of: Researcher, Lecturer, Student.", ErrorCodes.ValidationError);
            }

            var existingUser = await _accountRepository.GetUserByEmailAsync(email);
            if (existingUser != null)
            {
                return ServiceResult<RegisterResponse>.Fail(
                    "Email already exists.",
                    ErrorCodes.ResourceConflict,
                    StatusCodes.Status409Conflict);
            }

            var user = new User
            {
                Email = email,
                PasswordHash = HashPassword(password),
                FullName = fullName,
                DateOfBirth = request.DateOfBirth.Value.Date,
                PhoneNumber = phoneNumber,
                ActorType = actorType,
                CreatedAt = DateTime.UtcNow,
                IsActive = false
            };

            var createdUser = await _accountRepository.CreateUserAsync(user);
            var rawToken = GenerateEmailVerificationToken();
            await _accountRepository.AddEmailVerificationTokenAsync(new EmailVerificationToken
            {
                UserId = createdUser.UserId,
                TokenHash = HashToken(rawToken),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(EmailVerificationTokenHours)
            });

            await SendVerificationEmailAsync(createdUser.Email, rawToken);

            var response = new RegisterResponse
            {
                UserId = createdUser.UserId,
                Email = createdUser.Email,
                FullName = createdUser.FullName,
                DateOfBirth = createdUser.DateOfBirth,
                PhoneNumber = createdUser.PhoneNumber,
                ActorType = createdUser.ActorType,
                CreatedAt = createdUser.CreatedAt,
                IsActive = createdUser.IsActive ?? false,
                Message = "Registration successful. Please check your email to activate your account."
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
                return ServiceResult<LoginResponse>.Fail(
                    InvalidCredentialsMessage,
                    ErrorCodes.InvalidCredentials,
                    StatusCodes.Status401Unauthorized);
            }

            var email = request.Email.Trim();
            var user = await _accountRepository.GetUserByEmailAsync(email);
            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            {
                return ServiceResult<LoginResponse>.Fail(
                    InvalidCredentialsMessage,
                    ErrorCodes.InvalidCredentials,
                    StatusCodes.Status401Unauthorized);
            }

            if (user.IsActive != true)
            {
                return ServiceResult<LoginResponse>.Fail(
                    "Please verify your email before logging in.",
                    ErrorCodes.EmailNotVerified,
                    StatusCodes.Status403Forbidden);
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["JWT:Secret"] ?? "SuperSecretKeyForJWTWhichMustBeMoreThan16Chars!");
            
            // [CẬP NHẬT] Tạo danh sách Claims chứa thông tin cơ bản
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName ?? string.Empty),
                new Claim("actor_type", user.ActorType)
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
                UserId = user.UserId,
                Email = user.Email,
                FullName = user.FullName,
                ActorType = user.ActorType,
                Roles = user.Roles.Select(r => r.RoleName).ToList()
            });
        }

        public async Task<ServiceResult<string>> LogoutAsync(string token, int userId)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return ServiceResult<string>.Fail("Token is required.", ErrorCodes.Unauthorized, StatusCodes.Status401Unauthorized);
            }

            JwtSecurityToken jwtToken;
            try
            {
                jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token);
            }
            catch
            {
                return ServiceResult<string>.Fail("Invalid token.", ErrorCodes.Unauthorized, StatusCodes.Status401Unauthorized);
            }

            if (jwtToken.ValidTo <= DateTime.UtcNow)
            {
                return ServiceResult<string>.Fail("Token is already expired.", ErrorCodes.Unauthorized, StatusCodes.Status401Unauthorized);
            }

            await _accountRepository.RevokeTokenAsync(HashToken(token), userId, jwtToken.ValidTo);
            return ServiceResult<string>.Ok("Logout successful.");
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

        public async Task<ServiceResult<string>> VerifyEmailAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return ServiceResult<string>.Fail("Verification token is required.", ErrorCodes.ValidationError);
            }

            var tokenHash = HashToken(token.Trim());
            var verificationToken = await _accountRepository.GetValidEmailVerificationTokenAsync(tokenHash);
            if (verificationToken == null)
            {
                return ServiceResult<string>.Fail("Verification link is invalid or expired.", ErrorCodes.ValidationError);
            }

            await _accountRepository.MarkEmailVerifiedAsync(verificationToken);
            return ServiceResult<string>.Ok("Email verified successfully. Your account is now active.");
        }

        public async Task<ServiceResult<string>> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var email = request?.Email?.Trim();
            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            {
                return ServiceResult<string>.Fail("A valid email is required.", ErrorCodes.ValidationError);
            }

            var user = await _accountRepository.GetUserByEmailAsync(email);

            // Always return 200 to avoid revealing whether the email exists
            if (user == null || user.IsActive != true)
            {
                return ServiceResult<string>.Ok("If your email is registered and active, you will receive a PIN shortly.");
            }

            await _accountRepository.InvalidatePreviousPinsAsync(user.UserId);

            var pin = Random.Shared.Next(100000, 1000000).ToString();
            await _accountRepository.AddPasswordResetPinAsync(new Entities.PasswordResetPin
            {
                UserId = user.UserId,
                PinHash = HashToken(pin),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(15)
            });

            await SendPasswordResetPinEmailAsync(email, pin);

            return ServiceResult<string>.Ok("If your email is registered and active, you will receive a PIN shortly.");
        }

        public async Task<ServiceResult<string>> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var email = request?.Email?.Trim();
            var pin = request?.Pin?.Trim();
            var newPassword = request?.NewPassword ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            {
                return ServiceResult<string>.Fail("A valid email is required.", ErrorCodes.ValidationError);
            }

            if (string.IsNullOrWhiteSpace(pin) || pin.Length != 6 || !pin.All(char.IsDigit))
            {
                return ServiceResult<string>.Fail("PIN must be exactly 6 digits.", ErrorCodes.ValidationError);
            }

            if (newPassword.Length < MinPasswordLength)
            {
                return ServiceResult<string>.Fail("Password must be at least 6 characters.", ErrorCodes.ValidationError);
            }

            if (newPassword.Length > MaxPasswordLength)
            {
                return ServiceResult<string>.Fail("Password is too long.", ErrorCodes.ValidationError);
            }

            var user = await _accountRepository.GetUserByEmailAsync(email);
            if (user == null)
            {
                return ServiceResult<string>.Fail(
                    "Invalid or expired PIN.",
                    ErrorCodes.ValidationError,
                    StatusCodes.Status400BadRequest);
            }

            var pinHash = HashToken(pin);
            var resetPin = await _accountRepository.GetValidPasswordResetPinAsync(user.UserId, pinHash);
            if (resetPin == null)
            {
                return ServiceResult<string>.Fail(
                    "Invalid or expired PIN.",
                    ErrorCodes.ValidationError,
                    StatusCodes.Status400BadRequest);
            }

            await _accountRepository.ResetPasswordAsync(resetPin, HashPassword(newPassword));

            return ServiceResult<string>.Ok("Password reset successful.");
        }

        private async Task SendPasswordResetPinEmailAsync(string recipientEmail, string pin)
        {
            var host = _configuration["Email:SmtpHost"];
            var username = _configuration["Email:SmtpUsername"];
            var password = _configuration["Email:SmtpPassword"];
            var from = _configuration["Email:From"] ?? username;

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(from))
            {
                throw new InvalidOperationException("Email SMTP settings are not fully configured.");
            }

            var port = int.TryParse(_configuration["Email:SmtpPort"], out var configuredPort) ? configuredPort : 587;
            var enableSsl = !bool.TryParse(_configuration["Email:EnableSsl"], out var configuredSsl) || configuredSsl;

            using var message = new MailMessage(from, recipientEmail)
            {
                Subject = "Reset your Scientific Trend password",
                Body = $"Your password reset PIN is: {pin}\nThis PIN expires in 15 minutes.\nIf you did not request this, please ignore this email.",
                IsBodyHtml = false
            };

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = new System.Net.NetworkCredential(username, password)
            };

            await client.SendMailAsync(message);
        }

        private static string GenerateEmailVerificationToken()
        {
            var bytes = new byte[32];
            RandomNumberGenerator.Fill(bytes);
            return WebEncoders.Base64UrlEncode(bytes);
        }

        public static string HashToken(string token)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(hash);
        }

        private async Task SendVerificationEmailAsync(string recipientEmail, string token)
        {
            var verifyBaseUrl = _configuration["Email:VerifyBaseUrl"];
            if (string.IsNullOrWhiteSpace(verifyBaseUrl))
            {
                throw new InvalidOperationException("Email:VerifyBaseUrl is not configured.");
            }

            var host = _configuration["Email:SmtpHost"];
            var username = _configuration["Email:SmtpUsername"];
            var password = _configuration["Email:SmtpPassword"];
            var from = _configuration["Email:From"] ?? username;
            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(from))
            {
                throw new InvalidOperationException("Email SMTP settings are not fully configured.");
            }

            var verifyLink = $"{verifyBaseUrl.TrimEnd('/')}?token={Uri.EscapeDataString(token)}";
            var port = int.TryParse(_configuration["Email:SmtpPort"], out var configuredPort)
                ? configuredPort
                : 587;
            var enableSsl = !bool.TryParse(_configuration["Email:EnableSsl"], out var configuredSsl) || configuredSsl;

            using var message = new MailMessage(from, recipientEmail)
            {
                Subject = "Verify your Scientific Trend account",
                Body = $"Click this link to verify your account: {verifyLink}",
                IsBodyHtml = false
            };

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = new System.Net.NetworkCredential(username, password)
            };

            await client.SendMailAsync(message);
        }

        public async Task<ServiceResult<UserProfileResponse>> GetProfileAsync(int userId)
        {
            var user = await _accountRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return ServiceResult<UserProfileResponse>.Fail(
                    "User not found.",
                    ErrorCodes.ValidationError,
                    Microsoft.AspNetCore.Http.StatusCodes.Status404NotFound);
            }

            var response = new UserProfileResponse
            {
                UserId = user.UserId,
                Email = user.Email,
                FullName = user.FullName,
                ActorType = user.ActorType,
                RemainingCredits = user.RemainingCredits,
                LastCreditResetTime = user.LastCreditResetTime,
                Roles = user.Roles.Select(r => r.RoleName).ToList()
            };

            return ServiceResult<UserProfileResponse>.Ok(response);
        }
    }
}
