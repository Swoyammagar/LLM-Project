using OllamaSharp.Models.Chat;
using System;

namespace Backend.Models
{
    public class Conversation
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Soft delete flag. If true, conversation is logically deleted but data remains.
        /// Allows for recovery and audit trails.
        /// </summary>
        public bool IsDeleted { get; set; } = false;
        public User User { get; set; } = null!;
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}