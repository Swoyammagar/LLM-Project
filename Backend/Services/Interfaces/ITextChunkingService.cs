namespace Backend.Services.Interfaces
{
    public interface ITextChunkingService
    {
        Task<List<string>> ChunkTextAsync(string text);
    }
}
