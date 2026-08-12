namespace Backend.Services.Interfaces
{
    using Backend.DTOs.Chat;
    using Backend.Models;
    using Backend.DTOs.Search;
    public interface IConversationService
    {
        Task<Message> SaveMessageAsync(
            Guid userId,
            string question,
            string answer,
            string retrievedContext,
            List<RetrievedChunkDto> retrievedChunks,
            Guid? conversationId = null,
            string? conversationTitle = null);
        Task<List<ConversationDto>> GetConversationsAsync(
            Guid userId,
            int skip = 0,
            int take = 20);
        Task<ConversationDetailDto?> GetConversationAsync(
            Guid userId,
            Guid conversationId);

        Task<bool> DeleteConversationAsync(
            Guid userId,
            Guid conversationId);

        Task<int> GetConversationCountAsync(Guid userId);
    }
}
