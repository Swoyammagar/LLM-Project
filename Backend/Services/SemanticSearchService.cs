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
            float? similarityThreshold = null,
            Guid? documentId = null)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                throw new ArgumentException("Question cannot be null or empty.", nameof(question));
            }

            int resultLimit = topK ?? _options.DefaultTopK;
            float threshold = similarityThreshold ?? _options.DefaultSimilarityThreshold;

            try
            {
                var questionEmbedding = await _embeddingService.GenerateEmbeddingAsync(question);

                if (questionEmbedding == null)
                {
                    _logger.LogWarning("Failed to generate embedding for question. User: {UserId}", userId);
                    return new List<SemanticSearchResultDto>();
                }

                // documentId is nullable — "(${documentId} IS NULL OR d."Id" = ${documentId})"
                // lets one parameterized query cover both "search everything" and
                // "search just this document" without string-concatenating SQL.
                var results = await _context.Database
                    .SqlQuery<SemanticSearchResultDto>($@"
                        SELECT dc.""Id"" AS ""ChunkId"",
                            dc.""DocumentId"" AS ""DocumentId"",
                            dc.""Content"" AS ""Content"",
                            (1 - (dc.""Embedding"" <=> {questionEmbedding})) AS ""SimilarityScore""
                        FROM ""DocumentChunks"" dc
                        INNER JOIN ""Documents"" d ON dc.""DocumentId"" = d.""Id""
                        WHERE d.""UserId"" = {userId}
                        AND dc.""Embedding"" IS NOT NULL
                        AND ({documentId}::uuid IS NULL OR d.""Id"" = {documentId}::uuid)
                        ORDER BY dc.""Embedding"" <=> {questionEmbedding}
                        LIMIT {resultLimit}
                    ")
                    .ToListAsync();

                var topResults = results
                    .Where(r => r.SimilarityScore >= threshold)
                    .ToList();

                _logger.LogInformation(
                    "Semantic search completed. User: {UserId}, DocumentId: {DocumentId}, Results found: {ResultCount}",
                    userId, documentId?.ToString() ?? "ALL", topResults.Count);

                return topResults;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during semantic search. User: {UserId}, Query: {Query}", userId, question);
                throw;
            }
        }
    }
}