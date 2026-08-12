using System;

namespace Backend.Models
{
    public class Message
    {
        public Guid Id { get; set; }
        public Guid ConversationId { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string RetrievedContext { get; set; } = string.Empty;
        public string DocumentReferences { get; set; } = "[]";
        public int ChunksUsed { get; set; }
        public int TokensUsed { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Conversation Conversation { get; set; } = null!;
    }
}