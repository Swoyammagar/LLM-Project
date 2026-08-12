
using Backend.Data;
using Backend.DTOs.Chat;
using Backend.DTOs.Search;
using Backend.Models;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Backend.Services
{
    public class ConversationService : IConversationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ConversationService> _logger;

        public ConversationService(ApplicationDbContext context, ILogger<ConversationService> logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<Message> SaveMessageAsync(
            Guid userId,
            string question,
            string answer,
            string retrievedContext,
            List<RetrievedChunkDto> retrievedChunks,
            Guid? conversationId = null,
            string? conversationTitle = null)
        {
            try
            {
                Conversation conversation;
                if (conversationId == null || conversationId == Guid.Empty)
                {
                    string title = conversationTitle ??
                        (question.Length > 50 ? question.Substring(0, 50) + "..." : question);

                    conversation = new Conversation
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        Title = title,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsDeleted = false
                    };
                    _context.Conversations.Add(conversation);
                    _logger.LogInformation(
                        "Created new conversation {ConversationId} for user {UserId}",
                        conversation.Id, userId);
                }
                else
                {
                    conversation = await _context.Conversations
                        .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId && !c.IsDeleted);

                    if (conversation == null)
                    {
                        throw new InvalidOperationException(
                            $"Conversation {conversationId} not found or you don't have access.");
                    }
                    conversation.UpdatedAt = DateTime.UtcNow;
                }
                string documentReferencesJson = BuildDocumentReferencesJson(retrievedChunks);

                // Create message
                var message = new Message
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversation.Id,
                    Question = question,
                    Answer = answer,
                    RetrievedContext = retrievedContext,
                    DocumentReferences = documentReferencesJson,
                    ChunksUsed = retrievedChunks.Count,
                    TokensUsed = EstimateTokenCount(retrievedContext),
                    CreatedAt = DateTime.UtcNow
                };
                _context.Messages.Add(message);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Message saved");
                return message;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error saving message. User: {UserId}, ConversationId: {ConversationId}",
                    userId, conversationId);
                throw;
            }
        }
        public async Task<List<ConversationDto>> GetConversationsAsync(
            Guid userId,
            int skip = 0,
            int take = 20)
        {
            try
            {
                var conversations = await _context.Conversations
                    .Where(c => c.UserId == userId && !c.IsDeleted)
                    .OrderByDescending(c => c.UpdatedAt)
                    .Skip(skip)
                    .Take(take)
                    .Select(c => new ConversationDto
                    {
                        Id = c.Id,
                        Title = c.Title,
                        Description = c.Description,
                        MessageCount = c.Messages.Count,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt
                    })
                    .ToListAsync();
                return conversations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversations for user {UserId}", userId);
                throw;
            }
        }
        public async Task<ConversationDetailDto?> GetConversationAsync(
            Guid userId,
            Guid conversationId)
        {
            try
            {
                var conversation = await _context.Conversations
                   .Where(c => c.Id == conversationId && c.UserId == userId && !c.IsDeleted)
                   .Include(c => c.Messages.OrderBy(m => m.CreatedAt))
                   .FirstOrDefaultAsync();

                if (conversation == null)
                {
                    _logger.LogWarning(
                        "Conversation not found. ConversationId: {ConversationId}, UserId: {UserId}",
                        conversationId, userId);
                    return null;
                }

                var detail = new ConversationDetailDto
                {
                    Id = conversation.Id,
                    Title = conversation.Title,
                    Description = conversation.Description,
                    CreatedAt = conversation.CreatedAt,
                    UpdatedAt = conversation.UpdatedAt,
                    Messages = conversation.Messages
                        .Select(m => new MessageDto
                        {
                            Id = m.Id,
                            Question = m.Question,
                            Answer = m.Answer,
                            ChunksUsed = m.ChunksUsed,
                            TokensUsed = m.TokensUsed,
                            CreatedAt = m.CreatedAt
                        })
                        .ToList()
                };

                _logger.LogInformation(
                    "Retrieved conversation with {MessageCount} messages. ConversationId: {ConversationId}",
                    detail.Messages.Count, conversationId);

                return detail;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving conversation {ConversationId} for user {UserId}", conversationId, userId);
                throw;
            }
        }
        public async Task<bool> DeleteConversationAsync(
            Guid userId,
            Guid conversationId)
        {
            try
            {
                _logger.LogInformation(
                    "Deleting conversation {ConversationId} for user {UserId}",
                    conversationId, userId);

                var conversation = await _context.Conversations
                    .FirstOrDefaultAsync(c => c.Id == conversationId && c.UserId == userId);

                if (conversation == null)
                {
                    _logger.LogWarning(
                        "Conversation not found for deletion. ConversationId: {ConversationId}, UserId: {UserId}",
                        conversationId, userId);
                    return false;
                }

                // Soft delete
                conversation.IsDeleted = true;
                conversation.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Conversation deleted. ConversationId: {ConversationId}",
                    conversationId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error deleting conversation {ConversationId} for user {UserId}",
                    conversationId, userId);
                throw;
            }
        }
        public async Task<int> GetConversationCountAsync(Guid userId)
        {
            try
            {
                var count = await _context.Conversations
                    .CountAsync(c => c.UserId == userId && !c.IsDeleted);

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error getting conversation count for user {UserId}",
                    userId);
                throw;
            }
        }

        private string BuildDocumentReferencesJson(List<RetrievedChunkDto> retrievedChunks)
        {
            // Group chunks by document ID
            var groupedByDocument = retrievedChunks
                .GroupBy(rc => rc.DocumentId)
                .Select(g => new
                {
                    documentId = g.Key,
                    chunkIds = g.Select(rc => rc.ChunkId).ToList()
                })
                .ToList();

            // Serialize to JSON
            return JsonSerializer.Serialize(groupedByDocument);
        }

        /// <summary>
        /// Estimates token count for the context string.
        /// Uses rough approximation: 1 token ≈ 4 characters.
        /// </summary>
        private int EstimateTokenCount(string text)
        {
            return (text.Length / 4) + 1;
        }
    }
}
