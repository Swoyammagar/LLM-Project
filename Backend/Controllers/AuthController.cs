using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.Services;
using Backend.DTOs.Auth;
using System.Security.Claims;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly GoogleAuthService _googleAuthService;
        private readonly ILogger<AuthController> _logger;
        private readonly JwtTokenService _jwtTokenService;
        private readonly EmailAuthService _emailAuthService;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        public AuthController(
            GoogleAuthService googleAuthService,
            ILogger<AuthController> logger,
            JwtTokenService jwtTokenService,
            EmailAuthService emailAuthService,
            IConfiguration configuration,
            IWebHostEnvironment env)
        {
            _googleAuthService = googleAuthService;
            _logger = logger;
            _jwtTokenService = jwtTokenService;
            _emailAuthService = emailAuthService;
            _configuration = configuration;
            _env = env;
        }

        // --- cookie helpers ---

        private CookieOptions BuildCookieOptions(DateTime expires)
        {
            var cookieDomain = _configuration["Cookie:Domain"];
            return new CookieOptions
            {
                HttpOnly = true,
                Secure = !_env.IsDevelopment(),   // true (HTTPS) in prod, false over local http
                SameSite = SameSiteMode.Lax,      // fine as long as frontend + API share a root domain
                Expires = expires,
                Path = "/",
                Domain = string.IsNullOrWhiteSpace(cookieDomain) ? null : cookieDomain
            };
        }

        private void SetAuthCookies(string accessToken, string refreshToken)
        {
            Response.Cookies.Append("accessToken", accessToken, BuildCookieOptions(DateTime.UtcNow.AddMinutes(15)));
            Response.Cookies.Append("refreshToken", refreshToken, BuildCookieOptions(DateTime.UtcNow.AddDays(30)));
        }

        private void ClearAuthCookies()
        {
            Response.Cookies.Delete("accessToken", BuildCookieOptions(DateTime.UtcNow));
            Response.Cookies.Delete("refreshToken", BuildCookieOptions(DateTime.UtcNow));
        }

        [HttpPost("google")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleLogin(GoogleLoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.IdToken))
                {
                    return BadRequest(new { message = "IdToken is required" });
                }

                var result = await _googleAuthService.GoogleLoginOrSignupAsync(request.IdToken);
                SetAuthCookies(result.AccessToken, result.RefreshToken);

                return Ok(new AuthClientResponseDto { User = result.User, IsNewUser = result.IsNewUser });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid Google token");
                return Unauthorized(new { message = "Invalid Google token", error = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during Google authentication");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred during authentication" });
            }
        }

        [HttpPost("signup")]
        [AllowAnonymous]
        public async Task<IActionResult> Signup(SignupRequest request)
        {
            try
            {
                var result = await _emailAuthService.SignupAsync(request);
                SetAuthCookies(result.AccessToken, result.RefreshToken);

                return Ok(new AuthClientResponseDto { User = result.User, IsNewUser = result.IsNewUser });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Signup failed");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during signup");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred during signup" });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            try
            {
                var result = await _emailAuthService.LoginAsync(request);
                SetAuthCookies(result.AccessToken, result.RefreshToken);

                return Ok(new AuthClientResponseDto { User = result.User, IsNewUser = result.IsNewUser });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Login failed");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during login");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred during login" });
            }
        }

        [HttpPost("verify-email")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request)
        {
            try
            {
                var result = await _emailAuthService.VerifyEmailAsync(request);
                return Ok(new { message = "Email verified successfully", success = result });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Email verification failed");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during email verification");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred during email verification" });
            }
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                var refreshToken = Request.Cookies["refreshToken"];
                if (string.IsNullOrWhiteSpace(refreshToken))
                {
                    return Unauthorized(new { message = "No refresh token found" });
                }

                var result = await _emailAuthService.RefreshTokenAsync(refreshToken);
                SetAuthCookies(result.AccessToken, result.RefreshToken);

                return Ok(new { message = "Token refreshed successfully" });
            }
            catch (InvalidOperationException ex)
            {
                ClearAuthCookies();
                _logger.LogWarning(ex, "Refresh token invalid");
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during token refresh");
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred during token refresh" });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            if (!string.IsNullOrWhiteSpace(refreshToken))
            {
                await _emailAuthService.RevokeRefreshTokenAsync(refreshToken);
            }
            ClearAuthCookies();
            return Ok(new { message = "Logged out successfully" });
        }

        // Lets the frontend restore session state on page load without ever touching a token in JS
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var user = await _emailAuthService.GetCurrentUserAsync(userId);
            if (user == null)
            {
                return Unauthorized();
            }

            return Ok(user);
        }
    }
}