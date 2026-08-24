using Backend.Data;
using Backend.DTOs.Documents;
using Backend.Models;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;

namespace Backend.Services
{
    /// <summary>
    /// Service for managing document uploads, retrieval, and deletion.
    /// Now includes automatic text extraction, chunking, and embedding generation.
    /// </summary>
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DocumentService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly ITextExtractionService _textExtractionService;
        private readonly ITextChunkingService _textChunkingService;
        private readonly IEmbeddingService _embeddingService;

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

        /// <summary>
        /// Constructor injecting all services.
        /// Each service has a single responsibility (SRP - Single Responsibility Principle).
        /// </summary>
        public DocumentService(
            ApplicationDbContext context,
            ILogger<DocumentService> logger,
            IWebHostEnvironment environment,
            ITextExtractionService textExtractionService,
            ITextChunkingService textChunkingService,
            IEmbeddingService embeddingService)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
            _textExtractionService = textExtractionService;
            _textChunkingService = textChunkingService;
            _embeddingService = embeddingService;
        }

        /// <summary>
        /// Uploads a document, extracts text, generates chunks, and creates embeddings.
        /// 
        /// COMPLETE UPLOAD PIPELINE:
        /// ???????????????????????????????????????????????????????????????
        /// 
        /// 1. File Validation
        ///    - Check size, type, MIME
        ///    - Fail if invalid ?
        /// 
        /// 2. File Saved
        ///    - Save to Uploads/Documents/
        ///    - If fails, document upload fails ?
        /// 
        /// 3. Text Extraction (ITextExtractionService)
        ///    - Extract text from PDF/DOCX/TXT
        ///    - If fails, document saved but ExtractedText is null
        ///    - Upload continues ? (graceful degradation)
        /// 
        /// 4. Document Record Created
        ///    - Save to database with extracted text
        ///    - Upload continues regardless ?
        /// 
        /// 5. Chunking (ITextChunkingService)
        ///    - Split text into ~1000 char chunks
        ///    - If fails, document saved but no chunks
        ///    - Upload continues ?
        /// 
        /// 6. Embedding Generation (IEmbeddingService) ? NEW
        ///    - Generate vector for each chunk
        ///    - If fails for a chunk, that chunk's embedding stays null
        ///    - Upload continues ?
        /// 
        /// 7. Chunks Stored
        ///    - Save chunks with embeddings to database
        ///    - Upload continues ?
        /// 
        /// 8. Response Returned
        ///    - User gets success regardless of what failed downstream
        ///    - Logging provides detailed information of what succeeded/failed
        /// 
        /// FAILURE RESILIENCE:
        /// ???????????????????????????????????????????????????????????????
        /// File + Text + Chunks + Embeddings: ? Best case
        /// File + Text + Chunks (no embeddings): ? OK, can retry embeddings
        /// File + Text (no chunks): ? OK, can retry chunks+embeddings
        /// File (no text): ? OK, text extraction can retry
        /// No file: ? Only case upload fails
        /// </summary>
        public async Task<DocumentDto> UploadDocumentAsync(IFormFile file, Guid userId)
        {
            try
            {
                // ????????????????????????????????????????????????????????????????
                // STEP 1: FILE VALIDATION
                // ????????????????????????????????????????????????????????????????
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

                // ????????????????????????????????????????????????????????????????
                // STEP 2: SAVE FILE TO DISK
                // ????????????????????????????????????????????????????????????????
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

                // ????????????????????????????????????????????????????????????????
                // STEP 3: EXTRACT TEXT
                // ????????????????????????????????????????????????????????????????
                var extractedText = await _textExtractionService.ExtractTextAsync(filePath, fileExtension);

                // ????????????????????????????????????????????????????????????????
                // STEP 4: CREATE DOCUMENT RECORD
                // ????????????????????????????????????????????????????????????????
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
                    ExtractedText = string.IsNullOrEmpty(extractedText) ? null : extractedText
                };

                _context.Documents.Add(document);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Document metadata for {FileName} saved to database", uniqueFileName);

                // ????????????????????????????????????????????????????????????????
                // STEP 5: GENERATE CHUNKS AND EMBEDDINGS
                // ????????????????????????????????????????????????????????????????
                try
                {
                    await GenerateChunksWithEmbeddingsAsync(document, extractedText);
                }
                catch (Exception chunksEmbeddingsEx)
                {
                    // Log error but don't fail upload
                    _logger.LogError(chunksEmbeddingsEx,
                        "Failed to generate chunks and/or embeddings for document {DocumentId}, " +
                        "but document upload was successful. " +
                        "File and extracted text are preserved.",
                        document.Id);
                }

                return MapToDto(document);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Document upload validation failed: {Message}", ex.Message);
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

                _logger.LogInformation("Retrieved {Count} documents for user {UserId} (Page {PageNumber}, Size {PageSize})",
                    documents.Count, userId, pageNumber, pageSize);

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

        /// <summary>
        /// Generates chunks from extracted text and creates embeddings for each chunk.
        /// 
        /// EMBEDDING WORKFLOW:
        /// ???????????????????????????????????????????????????????????????
        /// 
        /// Input: "Machine learning is powerful AI technology. [1000 chars...]"
        /// 
        /// Step 1: Chunk the text
        /// ????????????????????????????????????????????????????
        /// ? Chunk 0: "Machine learning is powerful..." (1000) ?
        /// ? Chunk 1: "...technology. Deep learning is..." (1000) ?
        /// ? Chunk 2: "...neural networks..." (800) ?
        /// ????????????????????????????????????????????????????
        /// 
        /// Step 2: Generate embedding for each chunk
        /// ????????????????????????????????????????????????????
        /// ? Chunk 0 + Embedding ? Ollama generates vector    ?
        /// ? Chunk 1 + Embedding ? Ollama generates vector    ?
        /// ? Chunk 2 + Embedding ? Ollama generates vector    ?
        /// ????????????????????????????????????????????????????
        /// 
        /// Step 3: Store chunks with embeddings
        /// ????????????????????????????????????????????????????
        /// ? DocumentChunks table:                             ?
        /// ? ?? Id: guid                                       ?
        /// ? ?? DocumentId: guid                               ?
        /// ? ?? ChunkIndex: 0, 1, 2                            ?
        /// ? ?? Content: "Machine learning..."                 ?
        /// ? ?? Embedding: vector(384)                         ?
        /// ????????????????????????????????????????????????????
        /// 
        /// WHY EACH CHUNK NEEDS EMBEDDING:
        /// ???????????????????????????????????????????????????????????????
        /// 
        /// When user asks: "What is machine learning?"
        /// 
        /// Without embeddings:
        ///   - Search all chunks with keyword matching ?
        ///   - Miss semantically relevant chunks
        ///   - Poor Q&A quality
        /// 
        /// With embeddings:
        ///   - Query embedding: "What is machine learning?" ? vector
        ///   - Compare query vector to all chunk vectors (cosine similarity)
        ///   - Find most similar chunks
        ///   - Pass those chunks to LLM for answering ?
        ///   - Better Q&A quality with context
        /// </summary>
        private async Task GenerateChunksWithEmbeddingsAsync(Document document, string extractedText)
        {
            try
            {
                _logger.LogInformation("Starting chunk and embedding generation for document {DocumentId}", document.Id);

                // ????????????????????????????????????????????????????????????????
                // STEP 1: GENERATE CHUNKS
                // ????????????????????????????????????????????????????????????????
                var chunks = await _textChunkingService.ChunkTextAsync(extractedText ?? string.Empty);

                if (chunks.Count == 0)
                {
                    _logger.LogWarning("No chunks generated for document {DocumentId}. Text may be empty.", document.Id);
                    return;
                }

                _logger.LogInformation("Generated {ChunkCount} chunks for document {DocumentId}", chunks.Count, document.Id);

                // ????????????????????????????????????????????????????????????????
                // STEP 2: GENERATE EMBEDDINGS FOR ALL CHUNKS
                // ????????????????????????????????????????????????????????????????
                _logger.LogInformation("Starting embedding generation for {ChunkCount} chunks", chunks.Count);

                var embeddings = await _embeddingService.GenerateEmbeddingsAsync(chunks);

                if (embeddings.Count != chunks.Count)
                {
                    _logger.LogWarning(
                        "Embedding count ({EmbeddingCount}) doesn't match chunk count ({ChunkCount})",
                        embeddings.Count, chunks.Count);
                }

                // Count successful embeddings
                var successfulEmbeddings = embeddings.Count(e => e != null);
                _logger.LogInformation("Successfully generated {SuccessCount} embeddings out of {Total}",
                    successfulEmbeddings, chunks.Count);

                // ????????????????????????????????????????????????????????????????
                // STEP 3: CREATE DOCUMENT CHUNK ENTITIES WITH EMBEDDINGS
                // ????????????????????????????????????????????????????????????????
                var documentChunks = new List<DocumentChunk>();

                for (int i = 0; i < chunks.Count; i++)
                {
                    var documentChunk = new DocumentChunk
                    {
                        Id = Guid.NewGuid(),
                        DocumentId = document.Id,
                        ChunkIndex = i,
                        Content = chunks[i],
                        CharacterCount = chunks[i].Length,
                        CreatedAt = DateTime.UtcNow,
                        Embedding = embeddings[i] // Can be null if embedding failed
                    };

                    documentChunks.Add(documentChunk);

                    if (embeddings[i] != null)
                    {
                        _logger.LogInformation(
                            "Created chunk");
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Created chunk {Index}: {Chars} chars WITHOUT embedding (generation failed)",
                            i, chunks[i].Length);
                    }
                }

                // ????????????????????????????????????????????????????????????????
                // STEP 4: STORE CHUNKS WITH EMBEDDINGS IN DATABASE
                // ????????????????????????????????????????????????????????????????
                await _context.DocumentChunks.AddRangeAsync(documentChunks);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Successfully stored {ChunkCount} chunks with embeddings for document {DocumentId}. " +
                    "{EmbeddingCount} chunks have embeddings.",
                    documentChunks.Count, document.Id, successfulEmbeddings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error occurred while generating chunks and embeddings for document {DocumentId}. " +
                    "Document will be saved without chunks/embeddings.",
                    document.Id);
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

        public async Task<Document?> GetDocumentEntityByIdAsync(Guid documentId, Guid userId)
        {
            try
            {
                return await _context.Documents
                    .FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving document entity with ID {DocumentId} for user {UserId}", documentId, userId);
                throw;
            }
        }
    }
}