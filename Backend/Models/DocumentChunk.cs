using System;
using Pgvector;

namespace Backend.Models
{
    public class DocumentChunk
    {
        public Guid Id { get; set; }
        public Guid DocumentId { get; set; }
        public int ChunkIndex { get; set; }
        public string Content { get; set; } = string.Empty;
        public int CharacterCount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Document Document { get; set; } = null!;
        public Vector? Embedding { get; set; }
    }
}
