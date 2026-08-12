using Backend.Configuration;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models;
using Pgvector;

namespace Backend.Services
{
    public class OllamaEmbeddingService: IEmbeddingService
    {
        private readonly ILogger<OllamaEmbeddingService> _logger;
        private readonly OllamaOptions _options;
        private readonly OllamaApiClient _ollamaClient;

        public OllamaEmbeddingService(ILogger<OllamaEmbeddingService> logger, IOptions<OllamaOptions> options)
        {
            _logger = logger;
            _options = options.Value;
            _ollamaClient = new OllamaApiClient(
                new Uri(_options.BaseUrl), _options.EmbeddingModel);
        }

        public async Task<Vector?> GenerateEmbeddingAsync(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    _logger.LogWarning("Attempted to embed empty text");
                    return null;
                }

                _logger.LogInformation(
                    "Starting embedding generation for text ({CharCount} chars)",
                    text.Length);

                // EmbedAsync replaces the old GenerateEmbeddingAsync
                var response = await _ollamaClient.EmbedAsync(text);

                if (response?.Embeddings == null || response.Embeddings.Count == 0)
                {
                    _logger.LogError("Ollama returned empty embedding");
                    return null;
                }

                // Embeddings is a list of float[] (one per input) - take the first
                var vector = new Vector(response.Embeddings[0]);

                _logger.LogInformation(
                    "Successfully generated embedding");

                return vector;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex,
                    "HTTP error while contacting Ollama server at {BaseUrl}. " +
                    "Ensure Ollama is running: 'ollama serve'",
                    _options.BaseUrl);
                return null;
            }
            catch (TimeoutException ex)
            {
                _logger.LogError(ex,
                    "Timeout while generating embedding. Ollama may be overloaded or model loading is slow.");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while generating embedding");
                return null;
            }
        }
        public async Task<List<Vector?>> GenerateEmbeddingsAsync(List<string> texts)
        {
            try
            {
                if (texts == null || texts.Count == 0)
                {
                    _logger.LogWarning("Attempted to embed empty text list");
                    return new List<Vector?>();
                }

                _logger.LogInformation(
                    "Starting batch embedding generation for {Count} texts",
                    texts.Count);

                var request = new EmbedRequest
                {
                    Model = _options.EmbeddingModel,
                    Input = texts
                };

                var response = await _ollamaClient.EmbedAsync(request);

                var embeddings = response?.Embeddings?
                    .Select(e => e == null ? null : new Vector(e))
                    .ToList() ?? new List<Vector?>();

                _logger.LogInformation(
                    "Batch embedding complete. Generated {Count} embeddings",
                    embeddings.Count(e => e != null));

                return embeddings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch embedding generation");
                return texts.Select(_ => (Vector?)null).ToList();
            }
        }

    }
}
