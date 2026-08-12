namespace Backend.DTOs.Chat
{
    public class MessageDto
    {
        public Guid Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public int ChunksUsed { get; set; }
        public int TokensUsed { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}