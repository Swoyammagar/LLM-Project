namespace Backend.Configuration
{
    public class RetrievalOptions
    {
        public int MaxChunksToRetrieve { get; set; } = 5; // Maximum number of chunks to retrieve
        public int MaxContextCharacters { get; set; } = 3000; // Maximum character count for the combined context
        
        /// <summary>
        /// Character limit for each individual chunk preview in logs and responses.
        /// Default: 500
        /// Used for truncation in logging or optional chunk previews.
        /// </summary>
        public int MaxChunkPreviewCharacters { get; set; } = 500;

        /// <summary>
        /// Separator string used between chunks in the combined context.
        /// Default: "\n---\n"
        /// Clear separation helps the LLM distinguish between chunks.
        /// </summary>
        public string ChunkSeparator { get; set; } = "\n---\n";
    }
}
