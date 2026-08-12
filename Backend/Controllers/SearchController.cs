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
    public class SearchController : ControllerBase
    {
        private readonly ISemanticSearchService _semanticSearchService;
        private readonly ILogger<SearchController> _logger;

        /// <summary>
        /// Constructor injecting dependencies.
        /// </summary>
        public SearchController(
            ISemanticSearchService semanticSearchService,
            ILogger<SearchController> logger)
        {
            _semanticSearchService = semanticSearchService;
            _logger = logger;
        }

        [HttpPost("semantic")]
        public async Task<ActionResult<List<SemanticSearchResultDto>>> SemanticSearch(
            [FromBody] SemanticSearchRequest request,
            [FromQuery] int? topK = null,
            [FromQuery] float? similarityThreshold = null)
        {
            try
            {
                // Get authenticated user ID from JWT token
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

                // Validate topK parameter
                if (topK.HasValue && topK <= 0)
                {
                    return BadRequest("topK must be greater than 0.");
                }

                // Validate similarityThreshold parameter
                if (similarityThreshold.HasValue && (similarityThreshold < 0 || similarityThreshold > 1))
                {
                    return BadRequest("similarityThreshold must be between 0 and 1.");
                }

                _logger.LogInformation(
                    "Semantic search request from user {UserId}. Query: {Query}",
                    userId, request.Question);

                // Perform semantic search
                var results = await _semanticSearchService.SearchAsync(
                    userId,
                    request.Question,
                    topK,
                    similarityThreshold);

                return Ok(results);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument in semantic search");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing semantic search");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred during search");
            }
        }
    }

    /// <summary>
    /// Request model for semantic search.
    /// </summary>
    public class SemanticSearchRequest
    {
        public string Question { get; set; } = string.Empty;
    }
}