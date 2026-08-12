using Pgvector;

namespace Backend.Services.Interfaces
{
    public interface IEmbeddingService
    {
        Task<Vector?> GenerateEmbeddingAsync(string text);
        Task<List<Vector?>> GenerateEmbeddingsAsync(List<string> texts);
    }
}