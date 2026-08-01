namespace Backend.DTOs.Auth
{
    public class VerifyEmailRequest
    {
        public string Email { get; set; } = string.Empty;
        public string VerificationToken { get; set; } = string.Empty;
    }
}
