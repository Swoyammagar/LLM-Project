using Backend.Configuration;
using Backend.Services.Interfaces;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models;
using System.Text;

namespace Backend.Services
{
    public class OllamaLLMService : ILLMService
    {
        private readonly OllamaApiClient _client;
        private readonly OllamaLLMOptions _options;
        private readonly ILogger<OllamaLLMService> _logger;

        public OllamaLLMService(
            IOptions<OllamaLLMOptions> options,
            ILogger<OllamaLLMService> logger)
        {
            _options = options.Value;
            _logger = logger;

            _client = new OllamaApiClient(new Uri(_options.BaseUrl))
            {
                SelectedModel = _options.ChatModel
            };

            _logger.LogInformation(
                "OllamaLLMService initialized. Model: {Model}, BaseUrl: {BaseUrl}",
                _options.ChatModel, _options.BaseUrl);
        }

        /// <summary>
        /// Sends a prompt to Ollama and receives a completion.
        /// Uses the configured model and parameters.
        /// </summary>
        public async Task<string> CompleteAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Prompt cannot be null or empty.", nameof(prompt));
            }

            try
            {
                _logger.LogInformation(
                    "Sending prompt to Ollama. Model: {Model}, PromptLength: {PromptLength}",
                    _options.ChatModel, prompt.Length);

                using var cts = new CancellationTokenSource(
                    TimeSpan.FromSeconds(_options.TimeoutSeconds));

                var request = new GenerateRequest
                {
                    Model = _options.ChatModel,
                    Prompt = prompt,
                    Stream = false // still streams internally, but tells Ollama not to chunk the response
                };

                var responseBuilder = new StringBuilder();

                await foreach (var chunk in _client.GenerateAsync(request, cts.Token))
                {
                    if (chunk?.Response != null)
                    {
                        responseBuilder.Append(chunk.Response);
                    }
                }

                string answer = responseBuilder.ToString();

                _logger.LogInformation(
                    "Received completion from Ollama. ResponseLength: {ResponseLength}",
                    answer.Length);

                return answer;
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogError(ex,
                    "Timeout communicating with Ollama. Model: {Model}, TimeoutSeconds: {TimeoutSeconds}",
                    _options.ChatModel, _options.TimeoutSeconds);
                throw new InvalidOperationException(
                    $"LLM request timed out after {_options.TimeoutSeconds} seconds.", ex);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex,
                    "HTTP error communicating with Ollama. BaseUrl: {BaseUrl}",
                    _options.BaseUrl);
                throw new InvalidOperationException(
                    $"Failed to communicate with Ollama at {_options.BaseUrl}. " +
                    "Ensure Ollama is running and accessible.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error during LLM completion. Model: {Model}",
                    _options.ChatModel);
                throw new InvalidOperationException(
                    "An unexpected error occurred while generating LLM completion.", ex);
            }
        }
    }
}