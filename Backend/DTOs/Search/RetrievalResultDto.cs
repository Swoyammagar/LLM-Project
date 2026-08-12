namespace Backend.DTOs.Search
{
    public class RetrievalResultDto
    {
        public string Question { get; set; } = string.Empty;
        public List<RetrievedChunkDto> RetrievedChunks { get; set; } = new List<RetrievedChunkDto>();
        public string CombinedContext { get; set; } = string.Empty;
        public int ContextCharacterCount { get; set; }
        public int TotalChunksRetrieved { get; set; }
    }
}
