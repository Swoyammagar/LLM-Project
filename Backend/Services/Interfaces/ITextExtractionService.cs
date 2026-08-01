namespace Backend.Services.Interfaces
{
    public interface ITextExtractionService
    {
        Task<string> ExtractTextAsync(string filePath, string fileExtension);
    }
}