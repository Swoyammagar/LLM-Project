using Backend.Configuration;
using Backend.DTOs.Search;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Options;
using System.Text;

namespace Backend.Services
{
    public class RetrievalService : IRetrievalService
    {
        private readonly ISemanticSearchService _semanticSearchService;
        private readonly ILogger<RetrievalService> _logger;
        private readonly RetrievalOptions _options;
        public RetrievalService(
            ISemanticSearchService semanticSearchService,
            ILogger<RetrievalService> logger,
            IOptions<RetrievalOptions> options)
        {
            _semanticSearchService = semanticSearchService;
            _logger = logger;
            _options = options.Value;
        }
        public async Task<RetrievalResultDto> RetrieveAsync(
            Guid userId,
            string question,
            int? maxChunks = null,
            float? similarityThreshold = null)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(question))
            {
                throw new ArgumentException("Question cannot be null or empty.", nameof(question));
            }

            // Use provided values or fallback to configuration defaults
            int chunksLimit = maxChunks ?? _options.MaxChunksToRetrieve;
            int contextLimit = _options.MaxContextCharacters;

            try
            {
                _logger.LogInformation(
                    "Starting retrieval process for user {UserId}. Question: {Question}",
                    userId, question);

                // Step 1: Perform semantic search to find relevant chunks
                var semanticResults = await _semanticSearchService.SearchAsync(
                    userId,
                    question,
                    topK: chunksLimit,
                    similarityThreshold: similarityThreshold);

                _logger.LogInformation(
                    "Semantic search returned {ChunkCount} chunks for user {UserId}",
                    semanticResults.Count, userId);

                // Step 2: Build combined context from semantic search results
                var retrievalResult = BuildRetrievalResult(
                    question,
                    semanticResults,
                    contextLimit);

                _logger.LogInformation(
                    "Retrieval completed for user {UserId}. " +
                    "Chunks retrieved: {ChunkCount}, Context size: {ContextSize} characters",
                    userId, retrievalResult.TotalChunksRetrieved, retrievalResult.ContextCharacterCount);

                return retrievalResult;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument in retrieval. User: {UserId}", userId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error during retrieval process. User: {UserId}, Question: {Question}",
                    userId, question);
                throw;
            }
        }

        /// <summary>
        /// Builds a RetrievalResult by combining semantic search results into context.
        /// Respects the maximum context size limit by including chunks until limit is reached.
        /// </summary>
        private RetrievalResultDto BuildRetrievalResult(
            string question,
            List<SemanticSearchResultDto> semanticResults,
            int maxContextCharacters)
        {
            var result = new RetrievalResultDto
            {
                Question = question,
                RetrievedChunks = new List<RetrievedChunkDto>(),
                TotalChunksRetrieved = 0
            };

            // If no results, return empty retrieval result
            if (!semanticResults.Any())
            {
                _logger.LogWarning("No semantic search results returned");
                result.CombinedContext = string.Empty;
                result.ContextCharacterCount = 0;
                return result;
            }

            // Build context by combining chunks, respecting size limit
            var contextBuilder = new StringBuilder();
            int currentContextSize = 0;
            int retrievalIndex = 0;

            foreach (var searchResult in semanticResults)
            {
                // Calculate size if we add this chunk
                int chunkWithSeparatorSize = searchResult.Content.Length;

                // Add separator size if this isn't the first chunk
                if (contextBuilder.Length > 0)
                {
                    chunkWithSeparatorSize += _options.ChunkSeparator.Length;
                }

                // Check if adding this chunk would exceed the limit
                if (currentContextSize + chunkWithSeparatorSize > maxContextCharacters &&
                    retrievalIndex > 0) // Always include at least one chunk
                {
                    _logger.LogInformation(
                        "Context size limit reached. Included {ChunkCount} chunks. " +
                        "Context size: {ContextSize}/{MaxSize} characters",
                        retrievalIndex, currentContextSize, maxContextCharacters);
                    break;
                }

                // Add separator if not the first chunk
                if (contextBuilder.Length > 0)
                {
                    contextBuilder.Append(_options.ChunkSeparator);
                }

                // Add chunk content
                contextBuilder.Append(searchResult.Content);
                currentContextSize += chunkWithSeparatorSize;

                // Track retrieved chunk
                var retrievedChunk = new RetrievedChunkDto
                {
                    ChunkId = searchResult.ChunkId,
                    DocumentId = searchResult.DocumentId,
                    RetrievalIndex = retrievalIndex,
                    Content = searchResult.Content,
                    SimilarityScore = searchResult.SimilarityScore
                };

                result.RetrievedChunks.Add(retrievedChunk);
                retrievalIndex++;
            }

            result.CombinedContext = contextBuilder.ToString();
            result.ContextCharacterCount = result.CombinedContext.Length;
            result.TotalChunksRetrieved = result.RetrievedChunks.Count;

            return result;
        }
    }
}