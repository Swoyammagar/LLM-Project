using Google.Apis.Auth;
using Microsoft.EntityFrameworkCore;
using Backend.Data;
using Backend.Models;
using Backend.DTOs.Auth;

namespace Backend.Services;

public class GoogleAuthService
{
    private readonly ApplicationDbContext _dbcontext;
    private readonly IConfiguration _configuration;
    private readonly JwtTokenService _jwtTokenService;

    public GoogleAuthService(
        ApplicationDbContext dbcontext,
        IConfiguration configuration,
        JwtTokenService jwtTokenService)
    {
        _dbcontext = dbcontext;
        _configuration = configuration;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<GoogleJsonWebSignature.Payload> VerifyGoogleTokenAsync(string idToken)
    {
        return await GoogleJsonWebSignature.ValidateAsync(
            idToken,
            new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[]
                {
                    _configuration["Google:ClientId"]!
                }
            });
    }

    public async Task<AuthResponseDto> GoogleLoginOrSignupAsync(string idToken)
    {
        var googlePayload = await VerifyGoogleTokenAsync(idToken);

        // First search by GoogleId
        var user = await _dbcontext.Users
            .FirstOrDefaultAsync(x => x.GoogleId == googlePayload.Subject);

        // If not found, search by email
        if (user == null)
        {
            user = await _dbcontext.Users
                .FirstOrDefaultAsync(x => x.Email == googlePayload.Email);
        }

        bool isNewUser = false;

        if (user == null)
        {
            isNewUser = true;

            user = new User
            {
                Id = Guid.NewGuid(),
                GoogleId = googlePayload.Subject,
                Email = googlePayload.Email,
                Name = googlePayload.Name,
                ProfilePicture = googlePayload.Picture,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _dbcontext.Users.Add(user);
        }
        else
        {
            // Link Google account if it wasn't linked before
            if (string.IsNullOrWhiteSpace(user.GoogleId))
            {
                user.GoogleId = googlePayload.Subject;
            }

            user.Name = googlePayload.Name;
            user.ProfilePicture = googlePayload.Picture;
            user.UpdatedAt = DateTime.UtcNow;
            user.IsActive = true;
        }

        var accessToken = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = refreshToken,
            User = user,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsRevoked = false
        };

        _dbcontext.RefreshTokens.Add(refreshTokenEntity);

        await _dbcontext.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            IsNewUser = isNewUser,
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
}