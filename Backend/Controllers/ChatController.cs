using Backend.DTOs.Chat;
using Backend.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly IConversationService _conversationService;
        private readonly ILogger<ChatController> _logger;
        public ChatController(
            IChatService chatService,
            IConversationService conversationService,
            ILogger<ChatController> logger)
        {
            _chatService = chatService;
            _conversationService = conversationService;
            _logger = logger;
        }
        [HttpPost("chat")]
        public async Task<ActionResult<ChatResponse>> Chat(
        [FromBody] ChatRequest request,
        [FromQuery] int? maxContextChunks = null,
        [FromQuery] float? similarityThreshold = null,
        [FromQuery] Guid? conversationId = null)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    _logger.LogWarning("Invalid or missing user ID in token");
                    return Unauthorized("Invalid authentication token");
                }

                if (string.IsNullOrWhiteSpace(request.Question))
                {
                    return BadRequest("Question cannot be empty.");
                }

                if (maxContextChunks.HasValue && maxContextChunks <= 0)
                {
                    return BadRequest("maxContextChunks must be greater than 0.");
                }

                if (similarityThreshold.HasValue && (similarityThreshold < 0 || similarityThreshold > 1))
                {
                    return BadRequest("similarityThreshold must be between 0 and 1.");
                }

                _logger.LogInformation(
                    "Chat request from user {UserId}. Question: {Question}, DocumentId: {DocumentId}",
                    userId, request.Question, request.DocumentId?.ToString() ?? "ALL");

                var response = await _chatService.ChatAsync(
                    userId,
                    request.Question,
                    maxContextChunks,
                    similarityThreshold,
                    conversationId,
                    request.DocumentId); // <-- from body, since it's tied to the question payload

                _logger.LogInformation("Chat response sent to user {UserId}.", userId);

                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument in chat");
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Operation error");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in chat endpoint");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your question");
            }
        }
        [HttpGet("conversations")]
        public async Task<ActionResult<List<ConversationDto>>> GetConversations(
            [FromQuery] int skip = 0,
            [FromQuery] int take = 20)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized("Invalid authentication token");
                }

                // Validate pagination parameters
                if (skip < 0 || take <= 0)
                {
                    return BadRequest("skip must be >= 0 and take must be > 0.");
                }

                _logger.LogInformation(
                    "Getting conversations for user {UserId}. Skip: {Skip}, Take: {Take}",
                    userId, skip, take);

                var conversations = await _conversationService.GetConversationsAsync(
                    userId, skip, take);

                return Ok(conversations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversations");
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "An error occurred while retrieving conversations");
            }
        }
        [HttpGet("conversations/{id}")]
        public async Task<ActionResult<ConversationDetailDto>> GetConversation(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized("Invalid authentication token");
                }

                _logger.LogInformation(
                    "Getting conversation {ConversationId} for user {UserId}",
                    id, userId);

                var conversation = await _conversationService.GetConversationAsync(userId, id);

                if (conversation == null)
                {
                    return NotFound("Conversation not found or you don't have access.");
                }

                return Ok(conversation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversation {ConversationId}", id);
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "An error occurred while retrieving the conversation");
            }
        }

        [HttpDelete("conversations/{id}")]
        public async Task<ActionResult> DeleteConversation(Guid id)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return Unauthorized("Invalid authentication token");
                }

                _logger.LogInformation(
                    "Deleting conversation {ConversationId} for user {UserId}",
                    id, userId);

                var deleted = await _conversationService.DeleteConversationAsync(userId, id);

                if (!deleted)
                {
                    return NotFound("Conversation not found or you don't have access.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting conversation {ConversationId}", id);
                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    "An error occurred while deleting the conversation");
            }
        }
    }
}