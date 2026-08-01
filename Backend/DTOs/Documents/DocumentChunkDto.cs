namespace Backend.DTOs.Documents
{
    public class DocumentChunkDto
    {
        public Guid Id { get; set; }
        public int ChunkIndex { get; set; }
        public int CharacterCount { get; set; }
        public string ContentPreview { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}