using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.DTOs.Documents;
using Backend.Services.Interfaces;
using System.Security.Claims;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DocumentController : ControllerBase
    {
        public readonly IDocumentService _documentService;
        public readonly ILogger<DocumentController> _logger;
        public readonly IWebHostEnvironment _environment;

        public DocumentController(IDocumentService documentService, ILogger<DocumentController> logger, IWebHostEnvironment environment)
        {
            _documentService = documentService;
            _logger = logger;
            _environment = environment;

        }

        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDocument([FromForm] IFormFile file)
        {
            try
            {
                // Extract user ID from JWT claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out Guid userId))
                {
                    _logger.LogWarning("Unable to extract user ID from claims");
                    return Unauthorized(new { message = "User ID not found in token" });
                }

                _logger.LogInformation($"Upload request from user: {userId}, filename: {file?.FileName}");

                // Call service to handle upload
                var documentDto = await _documentService.UploadDocumentAsync(file, userId);

                return Ok(new
                {
                    message = "Document uploaded successfully",
                    data = documentDto
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning($"Validation error: {ex.Message}");
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error uploading document", error = ex.Message });
            }
        }
        [HttpGet]
        public async Task<IActionResult> GetDocuments([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                // Extract user ID from JWT claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out Guid userId))
                {
                    _logger.LogWarning("Unable to extract user ID from claims");
                    return Unauthorized(new { message = "User ID not found in token" });
                }

                _logger.LogInformation($"Fetching documents for user: {userId}, page: {pageNumber}, size: {pageSize}");

                // Call service to retrieve documents
                var documentList = await _documentService.GetUserDocumentsAsync(userId, pageNumber, pageSize);

                return Ok(new
                {
                    message = "Documents retrieved successfully",
                    data = documentList
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error retrieving documents", error = ex.Message });
            }
        }
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetDocument(Guid id)
        {
            try
            {
                // Extract user ID from JWT claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out Guid userId))
                {
                    _logger.LogWarning("Unable to extract user ID from claims");
                    return Unauthorized(new { message = "User ID not found in token" });
                }

                _logger.LogInformation($"Fetching document: {id} for user: {userId}");

                // Call service to retrieve document
                var document = await _documentService.GetDocumentByIdAsync(id, userId);

                if (document == null)
                {
                    _logger.LogWarning($"Document {id} not found for user {userId}");
                    return NotFound(new { message = "Document not found" });
                }

                return Ok(new
                {
                    message = "Document retrieved successfully",
                    data = document
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error retrieving document", error = ex.Message });
            }
        }
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteDocument(Guid id)
        {
            try
            {
                // Extract user ID from JWT claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out Guid userId))
                {
                    _logger.LogWarning("Unable to extract user ID from claims");
                    return Unauthorized(new { message = "User ID not found in token" });
                }

                _logger.LogInformation($"Delete request for document: {id} by user: {userId}");

                // Call service to delete document
                var success = await _documentService.DeleteDocumentAsync(id, userId);

                if (!success)
                {
                    _logger.LogWarning($"Document {id} not found for user {userId}");
                    return NotFound(new { message = "Document not found" });
                }

                return Ok(new { message = "Document deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error deleting document", error = ex.Message });
            }
        }
        [HttpGet("{id:guid}/file")]
        public async Task<IActionResult> GetDocumentFile(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!Guid.TryParse(userIdClaim, out Guid userId))
                {
                    _logger.LogWarning("Unable to extract user ID from claims");
                    return Unauthorized(new { message = "User ID not found in token" });
                }
                var document = await _documentService.GetDocumentEntityByIdAsync(id, userId);
                if (document == null)
                {
                    _logger.LogWarning($"Document {id} not found for user {userId}");
                    return NotFound(new { message = "Document not found" });
                }
                var filePath = Path.Combine(_environment.ContentRootPath, document.FilePath);
                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning($"File for document {id} not found at path {filePath}");
                    return NotFound(new { message = "Document file not found" });
                }
                return PhysicalFile(filePath, document.ContentType ?? "application/pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document file");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "Error retrieving document file", error = ex.Message });
            }
        }
    }
}