namespace Backend.DTOs.Chat
{
    using Backend.DTOs.Search;
    public class ChatResponse
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;

        /// <summary>
        /// The document chunks that were retrieved and used for context.
        /// Provides transparency about the information the answer is based on.
        /// Allows users to verify sources.
        /// </summary>
        public List<RetrievedChunkDto> RetrievedChunks { get; set; } = new List<RetrievedChunkDto>();
        
        /// <summary>
        /// Total number of tokens sent to the LLM (approximately).
        /// Useful for tracking API usage.
        /// </summary>
        public int ContextTokensUsed { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public Guid ConversationId { get; set; }    
        public Guid MessageId { get; set; }
    }
}
