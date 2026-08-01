namespace Backend.Models
{
    public class User
    {
        public Guid Id { get; set; }

        public string GoogleId { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? ProfilePicture { get; set; }

        public string? PasswordHash { get; set; }

        public bool IsEmailVerified { get; set; } 

        public string? EmailVerificationToken { get; set; }

        public DateTime? EmailVerificationTokenExpiry { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        // Navigation Property
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

        public ICollection<Document> Documents { get; set; } = new List<Document>();
    }
}