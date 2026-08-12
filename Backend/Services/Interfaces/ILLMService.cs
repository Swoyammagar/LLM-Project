namespace Backend.Services.Interfaces
{
    public interface ILLMService
    {
        /// <summary>
        /// Sends a prompt to the LLM and receives a completion.
        /// This is a simple completion endpoint - not streaming.
        /// </summary>
        /// <param name="prompt">The prompt to send to the LLM.</param>
        /// <returns>The LLM's response/completion.</returns>
        /// <exception cref="InvalidOperationException">If communication with LLM fails.</exception>
        Task<string> CompleteAsync(string prompt);
    }
}
