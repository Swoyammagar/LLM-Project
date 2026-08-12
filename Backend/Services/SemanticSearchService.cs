using Backend.Configuration;
using Backend.Data;
using Backend.DTOs.Search;
using Backend.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Backend.Services
{
    public class SemanticSearchService : ISemanticSearchService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger<SemanticSearchOptions> _logger;
        private readonly SemanticSearchOptions _options;

        public SemanticSearchService(
            ApplicationDbContext context,
            IEmbeddingService embeddingService,
            IOptions<SemanticSearchOptions> options,
            ILogger<SemanticSearchOptions> logger)
        {
            _context = context;
            _embeddingService = embeddingService;
            _logger = logger;
            _options = options.Value;
        }

        public async Task<List<SemanticSearchResultDto>> SearchAsync(
            Guid userId,
            string question,
            int? topK = null,
            float? similarityThreshold = null)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                throw new ArgumentException("Question cannot be null or empty.", nameof(question));
            }

            // Use provided values or fallback to configuration defaults
            int resultLimit = topK ?? _options.DefaultTopK;
            float threshold = similarityThreshold ?? _options.DefaultSimilarityThreshold;

            try
            {
                // Step 1: Generate embedding for the user's question
                var questionEmbedding = await _embeddingService.GenerateEmbeddingAsync(question);

                if (questionEmbedding == null)
                {
                    _logger.LogWarning(
                        "Failed to generate embedding for question. User: {UserId}",
                        userId);
                    return new List<SemanticSearchResultDto>();
                }

                // Step 2: Perform vector similarity search directly in SQL using pgvector's
                // <=> operator (cosine distance). Similarity = 1 - distance.
                // SqlQuery<T> (EF Core 8+) maps raw SQL rows straight onto the DTO by column name.
                var results = await _context.Database
                    .SqlQuery<SemanticSearchResultDto>($@"
                        SELECT dc.""Id"" AS ""ChunkId"",
                               dc.""DocumentId"" AS ""DocumentId"",
                               dc.""Content"" AS ""Content"",
                               (1 - (dc.""Embedding"" <=> {questionEmbedding})) AS ""SimilarityScore""
                        FROM ""DocumentChunks"" dc
                        INNER JOIN ""Documents"" d ON dc.""DocumentId"" = d.""Id""
                        WHERE d.""UserId"" = {userId} AND dc.""Embedding"" IS NOT NULL
                        ORDER BY dc.""Embedding"" <=> {questionEmbedding}
                        LIMIT {resultLimit}
                    ")
                    .ToListAsync();

                // Step 3: Apply the similarity threshold in memory (cheap since we already limited rows in SQL)
                var topResults = results
                    .Where(r => r.SimilarityScore >= threshold)
                    .ToList();

                _logger.LogInformation(
                    "Semantic search completed. User: {UserId}, Results found: {ResultCount}",
                    userId, topResults.Count);

                return topResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error during semantic search. User: {UserId}, Query: {Query}",
                    userId, question);
                throw;
            }
        }
    }
}