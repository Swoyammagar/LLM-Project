namespace Backend.DTOs.Auth
{
    public class AuthClientResponseDto
    {
        public UserDto User { get; set; } = null!;
        public bool IsNewUser { get; set; }
    }
}
