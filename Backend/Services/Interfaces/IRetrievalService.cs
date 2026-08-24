using Backend.DTOs.Search;

namespace Backend.Services.Interfaces
{
    public interface IRetrievalService
    {
        Task<RetrievalResultDto> RetrieveAsync(
            Guid userId,
            string question,
            int? maxChunks = null,
            float? similarityThreshold = null,
            Guid? documentId = null);
    }
}
