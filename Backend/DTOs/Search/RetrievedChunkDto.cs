namespace Backend.DTOs.Search
{
    public class RetrievedChunkDto
    {
        public Guid ChunkId { get; set; }
        public Guid DocumentId { get; set; }
        public int RetrievalIndex { get; set; }
        public string Content { get; set; } = string.Empty;
        public float SimilarityScore { get; set; }
    }
}
