namespace Backend.Services.Interfaces
{
    using Backend.DTOs.Chat;
    public interface IChatService
    {
        Task<ChatResponse> ChatAsync(
            Guid userId,
            string question,
            int? maxContextChunks = null,
            float? similarityThreshold = null,
            Guid? conversationId = null);
    }
}

