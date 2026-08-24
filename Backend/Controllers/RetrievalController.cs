using Backend.DTOs.Search;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RetrievalController : ControllerBase
    {
        private readonly IRetrievalService _retrievalService;
        private readonly ILogger<RetrievalController> _logger;

        public RetrievalController(IRetrievalService retrievalService, ILogger<RetrievalController> logger)
        {
            _retrievalService = retrievalService;
            _logger = logger;
        }

        [HttpPost("retrieve")]
        public async Task<ActionResult<RetrievalResultDto>> Retrieve(
            [FromBody] RetrieveRequest request,
            [FromQuery] int? maxChunks = null,
            [FromQuery] float? similarityThreshold = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    _logger.LogWarning("Invalid or missing user ID in token");
                    return Unauthorized("Invalid authentication token");
                }
                // Validate request
                if (string.IsNullOrWhiteSpace(request.Question))
                {
                    return BadRequest("Question cannot be empty.");
                }

                // Validate maxChunks parameter
                if (maxChunks.HasValue && maxChunks <= 0)
                {
                    return BadRequest("maxChunks must be greater than 0.");
                }

                // Validate similarityThreshold parameter
                if (similarityThreshold.HasValue && (similarityThreshold < 0 || similarityThreshold > 1))
                {
                    return BadRequest("similarityThreshold must be between 0 and 1.");
                }

                _logger.LogInformation(
                    "Retrieval request from user {UserId}. Question: {Question}",
                    userId, request.Question);

                var result = await _retrievalService.RetrieveAsync(
                    userId, request.Question, maxChunks, similarityThreshold, request.DocumentId);

                _logger.LogInformation(
                    "Retrieval completed. User: {UserId}, Chunks retrieved: {ChunkCount}, Context size: {ContextSize}",
                    userId, result.TotalChunksRetrieved, result.ContextCharacterCount);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument in retrieval");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing retrieval");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred during retrieval");
            }
        }
        public class RetrieveRequest
        {
            public string Question { get; set; } = string.Empty;
            public Guid? DocumentId { get; set; }
            }

    }
}
