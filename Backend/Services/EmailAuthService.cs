using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.DTOs.Auth;

namespace Backend.Services
{
    public class EmailAuthService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly JwtTokenService _jwtTokenService;
        private readonly PasswordService _passwordService;
        private readonly ILogger<EmailAuthService> _logger;

        public EmailAuthService(
            ApplicationDbContext dbContext,
            JwtTokenService jwtTokenService,
            PasswordService passwordService,
            ILogger<EmailAuthService> logger)
        {
            _dbContext = dbContext;
            _jwtTokenService = jwtTokenService;
            _passwordService = passwordService;
            _logger = logger;
        }

        public async Task<AuthResponseDto> SignupAsync(SignupRequest request)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Name))
            {
                throw new InvalidOperationException("Email, password, and name are required");
            }

            if (request.Password.Length < 6)
            {
                throw new InvalidOperationException("Password must be at least 6 characters long");
            }

            // Check if user already exists
            var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
            {
                throw new InvalidOperationException("User with this email already exists");
            }

            // Create new user
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                Name = request.Name,
                PasswordHash = _passwordService.HashPassword(request.Password),
                EmailVerificationToken = _passwordService.GenerateVerificationToken(),
                EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"User signup successful for email: {request.Email}");

            var accessToken = _jwtTokenService.GenerateAccessToken(user);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            // Store refresh token
            await StoreRefreshTokenAsync(user.Id, refreshToken);

            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                ProfilePicture = user.ProfilePicture,
                CreatedAt = user.CreatedAt
            };

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = userDto,
                IsNewUser = true
            };
        }
        public async Task<AuthResponseDto> LoginAsync(LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            {
                throw new InvalidOperationException("Email and password are required");
            }
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                throw new InvalidOperationException("Invalid email or password");
            }
            if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash))
            {
                throw new InvalidOperationException("Invalid email or password");
            }

            user.UpdatedAt = DateTime.UtcNow;
            user.IsActive = true;
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"User login successful for email: {request.Email}");

            var accessToken = _jwtTokenService.GenerateAccessToken(user);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            await StoreRefreshTokenAsync(user.Id, refreshToken);

            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                ProfilePicture = user.ProfilePicture,
                CreatedAt = user.CreatedAt
            };
            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = userDto,
                IsNewUser = false
            };
        }
        public async Task<bool> VerifyEmailAsync(VerifyEmailRequest request)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                throw new InvalidOperationException("User not found");
            }
            if (user.EmailVerificationToken != request.VerificationToken)
            {
                throw new InvalidOperationException("Invalid verification token");
            }
            if (user.EmailVerificationTokenExpiry < DateTime.UtcNow)
            {
                throw new InvalidOperationException("Verification token has expired");
            }
            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpiry = null;
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Email verification successful for email: {request.Email}");
            return true;
        }

        public async Task StoreRefreshTokenAsync(Guid userId, string refreshToken)
        {
            var token = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = refreshToken,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7), // Set expiry as needed
            };
            _dbContext.RefreshTokens.Add(token);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> ValidateRefreshTokenAsync(Guid userId, string refreshToken)
        {
            var token = await _dbContext.Set<RefreshToken>()
                .FirstOrDefaultAsync(rt => rt.UserId == userId && rt.Token == refreshToken && !rt.IsRevoked);

            if (token == null || token.ExpiresAt < DateTime.UtcNow)
            {
                return false;
            }

            return true;
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(string refreshToken)
        {
            var existingToken = await _dbContext.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            if (existingToken == null)
            {
                throw new InvalidOperationException("Invalid refresh token");
            }

            if (existingToken.IsRevoked)
            {
                // This token was already used once before — someone is replaying an old token
                // (stolen cookie, race condition, etc). Treat as compromised.
                throw new InvalidOperationException("Refresh token has already been used");
            }

            if (existingToken.ExpiresAt < DateTime.UtcNow)
            {
                throw new InvalidOperationException("Refresh token has expired");
            }

            var user = existingToken.User;
            if (user == null || !user.IsActive)
            {
                throw new InvalidOperationException("User not found or inactive");
            }

            // Rotate: kill the old token, issue a brand-new one
            existingToken.IsRevoked = true;

            var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
            var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

            _dbContext.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                Token = newRefreshToken,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                IsRevoked = false
            });

            await _dbContext.SaveChangesAsync();

            return new AuthResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                IsNewUser = false,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    ProfilePicture = user.ProfilePicture,
                    CreatedAt = user.CreatedAt
                }
            };
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken)
        {
            var token = await _dbContext.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == refreshToken);
            if (token != null)
            {
                token.IsRevoked = true;
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<UserDto?> GetCurrentUserAsync(Guid userId)
        {
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null) return null;

            return new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                Name = user.Name,
                ProfilePicture = user.ProfilePicture,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
