namespace Backend.Configuration
{
    public class OllamaLLMOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:11434";
        public string ChatModel { get; set; } = "llama3.2:3b";

        /// <summary>
        /// Temperature parameter for LLM generation.
        /// Controls randomness/creativity of responses.
        /// Range: 0.0 to 2.0
        /// - 0.0: Deterministic, repeatable responses (best for fact-based)
        /// - 0.7: Balanced (default)
        /// - 1.5+: Creative, varied responses (worst for fact-based)
        /// Default: 0.3 (low, for accurate retrieval-based answers)
        /// </summary>
        public decimal Temperature { get; set; } = 0.3m;
        public int MaxTokens { get; set; } = 512;
        public int TimeoutSeconds { get; set; } = 300;

        /// <summary>
        /// System prompt that instructs the model behavior.
        /// Prepended to every request to guide the model.
        /// </summary>
        public string SystemPrompt { get; set; } = "You are a helpful assistant that answers questions based only on the provided context. Never invent information or answer from general knowledge when context is available. If you cannot answer from the context, clearly state that.";
    }
}