using Backend.Data;
using Backend.DTOs.Documents;
using Backend.Models;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;

namespace Backend.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DocumentService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly ITextExtractionService _textExtractionService;
        private readonly ITextChunkingService _textChunkingService;

        private static readonly HashSet<string> AllowedExtensions = new()
        {
            ".pdf", ".docx", ".txt"
        };

        private static readonly HashSet<string> AllowedMimeTypes = new()
        {
            "application/pdf",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "text/plain"
        };
        private const long MaxFileSize = 20 * 1024 * 1024;

        private const string UploadDirectory = "Uploads/Documents";
        public DocumentService(ApplicationDbContext context, ILogger<DocumentService> logger, IWebHostEnvironment environment, ITextExtractionService textExtractionService, ITextChunkingService textChunkingService)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
            _textExtractionService = textExtractionService;
            _textChunkingService = textChunkingService;
        }
        public async Task<DocumentDto> UploadDocumentAsync(IFormFile file, Guid userId)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    throw new InvalidOperationException("File is empty.");
                }
                if (file.Length > MaxFileSize)
                {
                    throw new InvalidOperationException("File size exceeds the maximum limit of 20 MB.");
                }
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(fileExtension))
                {
                    throw new InvalidOperationException("File type is not allowed.");
                }
                if (!AllowedMimeTypes.Contains(file.ContentType))
                {
                    throw new InvalidOperationException("File MIME type is not allowed.");
                }
                var uploadsPath = Path.Combine(_environment.ContentRootPath, UploadDirectory);
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                    _logger.LogInformation("Created uploads directory at {UploadsPath}", uploadsPath);
                }
                var uniqueFileName = GenerateUniqueFileName(file.FileName);
                var filePath = Path.Combine(uploadsPath, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                _logger.LogInformation("File {FileName} uploaded successfully to {FilePath}", uniqueFileName, filePath);

                var extractedText = await _textExtractionService.ExtractTextAsync(filePath, file.ContentType);

                var document = new Document
                {
                    Id = Guid.NewGuid(),
                    FileName = uniqueFileName,
                    OriginalFileName = file.FileName,
                    FilePath = Path.Combine(UploadDirectory, uniqueFileName),
                    FileSize = file.Length,
                    ContentType = file.ContentType,
                    UserId = userId,
                    UploadDate = DateTime.UtcNow,
                    ExtractedText = string.IsNullOrEmpty(extractedText)? null : extractedText
                };
                _context.Documents.Add(document);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Document metadata for {FileName} saved to database", uniqueFileName);
                
                try
                {
                    await GenerateAndStoreChunksAsync(document, extractedText);
                }
                catch (Exception chunkingEx)
                {
                    // Log the error but don't fail the upload
                    // This is intentional - we want the document and extracted text preserved
                    _logger.LogError(chunkingEx,
                        "Failed to generate chunks for document {DocumentId}, but document upload was successful. " +
                        "File and extracted text are preserved. Chunks can be regenerated later.",
                        document.Id);
                }
                return MapToDto(document);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Document upload validation failed: {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while uploading document");
                throw;
            }

        }
        public async Task<DocumentListDto> GetUserDocumentsAsync(Guid userId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1) pageSize = 10;
                if (pageSize > 100) pageSize = 100;

                var totalCount = await _context.Documents
                    .Where(d => d.UserId == userId)
                    .CountAsync();

                var documents = await _context.Documents
                    .Where(d => d.UserId == userId)
                    .OrderByDescending(d => d.UploadDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
                _logger.LogInformation("Retrieved {Count} documents for user {UserId} (Page {PageNumber}, Size {PageSize})", documents.Count, userId, pageNumber, pageSize);
                return new DocumentListDto
                {
                    Documents = documents.Select(MapToDto).ToList(),
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving documents for user {UserId}", userId);
                throw;
            }
        }
        public async Task<DocumentDto?> GetDocumentByIdAsync(Guid documentId, Guid userId)
        {
            try
            {
                var document = await _context.Documents
                    .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);
                if (document == null)
                {
                    _logger.LogWarning("Document with ID {DocumentId} not found for user {UserId}", documentId, userId);
                    return null;
                }
                _logger.LogInformation("Retrieved document with ID {DocumentId} for user {UserId}", documentId, userId);
                return MapToDto(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving document with ID {DocumentId} for user {UserId}", documentId, userId);
                throw;
            }
        }
        public async Task<bool> DeleteDocumentAsync(Guid documentId, Guid userId)
        {
            try
            {
                var document = await _context.Documents
                    .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);
                if (document == null)
                {
                    _logger.LogWarning("Document with ID {DocumentId} not found for user {UserId}", documentId, userId);
                    return false;
                }
                var filePath = Path.Combine(_environment.ContentRootPath, document.FilePath);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("Deleted file at {FilePath} for document ID {DocumentId}", filePath, documentId);
                }
                else
                {
                    _logger.LogWarning("File at {FilePath} not found for document ID {DocumentId}", filePath, documentId);
                }
                _context.Documents.Remove(document);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Deleted document metadata for ID {DocumentId} from database", documentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting document with ID {DocumentId} for user {UserId}", documentId, userId);
                throw;
            }
        }
        private static string GenerateUniqueFileName(string originalFileName)
        {
            var guid = Guid.NewGuid().ToString("N").Substring(0, 8);
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var extension = Path.GetExtension(originalFileName);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);

            return $"{guid}_{timestamp}_{nameWithoutExtension}{extension}";
        }
        private static DocumentDto MapToDto(Document document)
        {
            return new DocumentDto
            {
                Id = document.Id,
                OriginalFileName = document.OriginalFileName,
                FileSize = document.FileSize,
                ContentType = document.ContentType,
                UploadDate = document.UploadDate
            };
        }
        private async Task GenerateAndStoreChunksAsync(Document document, string extractedText)
        {
            try
            {
                var chunks = await _textChunkingService.ChunkTextAsync(extractedText ?? string.Empty);

                if (chunks.Count == 0)
                {
                    _logger.LogWarning("No chunks generated for document {DocumentId}. Text may be empty.", document.Id);
                    return;
                }
                var documentChunks = new List<DocumentChunk>();
                for (int i = 0; i < chunks.Count; i++)
                {
                    documentChunks.Add(new DocumentChunk
                    {
                        Id = Guid.NewGuid(),
                        DocumentId = document.Id,
                        ChunkIndex = i,
                        Content = chunks[i],
                        CharacterCount = chunks[i].Length,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                await _context.DocumentChunks.AddRangeAsync(documentChunks);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Generated and stored {Count} chunks for document {DocumentId}", documentChunks.Count, document.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while generating and storing chunks for document {DocumentId}", document.Id);
                throw;
            }
        }
    }
}