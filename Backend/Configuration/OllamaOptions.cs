namespace Backend.Configuration
{
    public class OllamaOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:11434";
        public string EmbeddingModel { get; set; } = "nomic-embed-text";

        /// <summary>
        /// Optional: Timeout for API requests (in seconds).
        /// Useful for handling slow network or model processing.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 300;
    }
}