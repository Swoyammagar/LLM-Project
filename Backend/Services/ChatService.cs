using Backend.Configuration;
using Backend.DTOs.Chat;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Text;

namespace Backend.Services
{
    public class ChatService : IChatService
    {
        private readonly IRetrievalService _retrievalService;
        private readonly ILLMService _llmService;
        private readonly IConversationService _conversationService;
        private readonly ILogger<ChatService> _logger;
        private readonly OllamaLLMOptions _ollamaOptions;

        public ChatService(
            IRetrievalService retrievalService,
            ILLMService llmService,
            IConversationService conversationService,
            ILogger<ChatService> logger,
            IOptions<OllamaLLMOptions> ollamaOptions)
        {
            _retrievalService = retrievalService;
            _llmService = llmService;
            _conversationService = conversationService;
            _logger = logger;
            _ollamaOptions = ollamaOptions.Value;
        }
        public async Task<ChatResponse> ChatAsync(
            Guid userId,
            string question,
            int? maxContextChunks = null,
            float? similarityThreshold = null,
            Guid? conversationId = null)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(question))
            {
                throw new ArgumentException("Question cannot be null or empty.", nameof(question));
            }

            try
            {
                _logger.LogInformation(
                    "Chat request from user {UserId}. Question: {Question}, ConversationId: {ConversationId}",
                    userId, question, conversationId ?? Guid.Empty);

                // ═════════════════════════════════════════════════════════════════
                // STEP 1: RETRIEVAL - Find relevant chunks from user's documents
                // ═════════════════════════════════════════════════════════════════
                _logger.LogInformation("Step 1: Retrieving relevant context...");

                var retrievalResult = await _retrievalService.RetrieveAsync(
                    userId,
                    question,
                    maxChunks: maxContextChunks,
                    similarityThreshold: similarityThreshold);

                _logger.LogInformation(
                    "Retrieved {ChunkCount} chunks. Context size: {ContextSize} characters",
                    retrievalResult.TotalChunksRetrieved,
                    retrievalResult.ContextCharacterCount);

                // ═════════════════════════════════════════════════════════════════
                // STEP 2: PROMPT BUILDING - Create contextualized prompt
                // ═════════════════════════════════════════════════════════════════
                _logger.LogInformation("Step 2: Building prompt with context...");

                var prompt = BuildPrompt(question, retrievalResult.CombinedContext);

                _logger.LogInformation(
                    "Prompt built. Total size: {PromptSize} characters",
                    prompt.Length);

                // ═════════════════════════════════════════════════════════════════
                // STEP 3: GENERATION - Send prompt to LLM
                // ═════════════════════════════════════════════════════════════════
                _logger.LogInformation(
                    "Step 3: Sending prompt to LLM ({Model})...",
                    _ollamaOptions.ChatModel);

                var answer = await _llmService.CompleteAsync(prompt);

                _logger.LogInformation(
                    "Received answer from LLM. Answer size: {AnswerSize} characters",
                    answer.Length);

                // ═════════════════════════════════════════════════════════════════
                // STEP 4: PERSISTENCE - Save message to conversation
                // ═════════════════════════════════════════════════════════════════
                _logger.LogInformation("Step 4: Saving message to conversation...");

                var savedMessage = await _conversationService.SaveMessageAsync(
                    userId,
                    question,
                    answer,
                    retrievalResult.CombinedContext,
                    retrievalResult.RetrievedChunks,
                    conversationId,
                    conversationTitle: null);

                _logger.LogInformation(
                    "Message saved. MessageId: {MessageId}, ConversationId: {ConversationId}",
                    savedMessage.Id, savedMessage.ConversationId);

                // ═════════════════════════════════════════════════════════════════
                // STEP 5: RESPONSE - Build and return chat response
                // ═════════════════════════════════════════════════════════════════
                var response = new ChatResponse
                {
                    Question = question,
                    Answer = answer,
                    RetrievedChunks = retrievalResult.RetrievedChunks,
                    ContextTokensUsed = EstimateTokenCount(prompt),
                    GeneratedAt = DateTime.UtcNow,
                    ConversationId = savedMessage.ConversationId,
                    MessageId = savedMessage.Id
                };

                _logger.LogInformation(
                    "Chat completed for user {UserId}. Answer generated and saved.",
                    userId);

                return response;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument in chat. User: {UserId}", userId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error during chat pipeline. User: {UserId}, Question: {Question}",
                    userId, question);
                throw;
            }
        }

        private string BuildPrompt(string question, string context)
        {
            var promptBuilder = new StringBuilder();

            // System instruction to guide model behavior
            promptBuilder.AppendLine("You are a helpful assistant that answers questions based ONLY on the provided context.");
            promptBuilder.AppendLine("Follow these strict rules:");
            promptBuilder.AppendLine("1. ONLY use information from the provided context to answer.");
            promptBuilder.AppendLine("2. If the answer is not in the context, say: 'I cannot find this information in the provided documents.'");
            promptBuilder.AppendLine("3. Do NOT use general knowledge when context is available.");
            promptBuilder.AppendLine("4. Do NOT invent, assume, or hallucinate any information.");
            promptBuilder.AppendLine("5. Be concise and factual.");
            promptBuilder.AppendLine();

            // Provided context section
            promptBuilder.AppendLine("===== PROVIDED CONTEXT =====");
            promptBuilder.AppendLine(context);
            promptBuilder.AppendLine("===== END CONTEXT =====");
            promptBuilder.AppendLine();

            // User question section
            promptBuilder.AppendLine("===== QUESTION =====");
            promptBuilder.AppendLine(question);
            promptBuilder.AppendLine("===== END QUESTION =====");
            promptBuilder.AppendLine();

            // Instruction to answer
            promptBuilder.AppendLine("Based ONLY on the context provided above, answer the question:");

            return promptBuilder.ToString();
        }

        private int EstimateTokenCount(string text)
        {
            return (text.Length / 4) + 1;
        }
    }
}