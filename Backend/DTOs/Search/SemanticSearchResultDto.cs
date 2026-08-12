namespace Backend.DTOs.Search
{
    public class SemanticSearchResultDto
    {
        public Guid ChunkId { get; set; }
        public Guid DocumentId { get; set; }
        public string Content { get; set; } = string.Empty;
        public float SimilarityScore { get; set; }
    }
}
