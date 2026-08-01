using Backend.DTOs.Documents;
using Backend.Models;

namespace Backend.Services.Interfaces
{
    /// <summary>
    /// Interface defining document management operations
    /// This follows the Dependency Inversion Principle - code depends on abstractions, not concrete implementations
    /// </summary>
    public interface IDocumentService
    {
        Task<DocumentDto> UploadDocumentAsync(IFormFile file, Guid userId);
        Task<DocumentListDto> GetUserDocumentsAsync(Guid userId, int pageNumber = 1, int pageSize = 10);
        Task<DocumentDto?> GetDocumentByIdAsync(Guid documentId, Guid userId);
        Task<bool> DeleteDocumentAsync(Guid documentId, Guid userId);
    }
}