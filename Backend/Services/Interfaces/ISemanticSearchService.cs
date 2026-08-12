using Backend.DTOs.Search;

namespace Backend.Services.Interfaces
{
    public interface ISemanticSearchService
    {
    Task<List<SemanticSearchResultDto>> SearchAsync(
        Guid userId,
        string question,
        int? topK = null,
        float? similarityThreshold = null);
    }
}
