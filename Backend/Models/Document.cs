using System;

namespace Backend.Models
{
    public class Document
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public Guid UserId { get; set; }
        public DateTime UploadDate { get; set; }= DateTime.UtcNow;
        public string? ExtractedText { get; set; }
        public User User { get; set; } = null!;
        public ICollection<DocumentChunk> Chunks {get; set;} = new List<DocumentChunk>();
    }
}
