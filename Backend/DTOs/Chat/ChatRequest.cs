namespace Backend.DTOs.Chat
{
    public class ChatRequest
    {
        public string Question { get; set; } = string.Empty;
        public int? MaxContextChunks { get; set; }
        public float? SimilarityThreshold { get; set; }
        public Guid? DocumentId { get; set; }

    }
}
