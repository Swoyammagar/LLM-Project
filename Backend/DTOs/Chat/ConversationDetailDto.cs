namespace Backend.DTOs.Chat
{
    /// <summary>
    /// DTO for a full conversation with all messages.
    /// Used in GET /conversations/{id} responses (detail view).
    /// </summary>
    public class ConversationDetailDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<MessageDto> Messages { get; set; } = new();
    }
}