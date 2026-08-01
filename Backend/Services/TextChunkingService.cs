using Backend.Services.Interfaces;

namespace Backend.Services
{
    public class TextChunkingService: ITextChunkingService
    {
        private readonly ILogger<TextChunkingService> _logger;
        
        private const int ChunkSize = 1000; // Define the chunk size (number of characters per chunk)
        private const int OverlapSize = 200; // Define the overlap size (number of characters to overlap between chunks)
        private const int MinChunkSize = 100; // Define the maximum chunk size (number of characters per chunk)
        public TextChunkingService(ILogger<TextChunkingService> logger)
        {
            _logger = logger;
        }

        public async Task<List<string>> ChunkTextAsync(string text)
        {
            return await Task.Run(() =>
            {
                try
                {// Handle empty/null input
                    if (string.IsNullOrWhiteSpace(text))
                    {
                        _logger.LogWarning("Received empty or null text for chunking");
                        return new List<string>();
                    }

                    _logger.LogInformation("Starting text chunking. Input text length: {TextLength} characters", text.Length);

                    var chunks = new List<string>();
                    var currentIndex = 0;

                    // If text is smaller than chunk size, return as single chunk
                    if (text.Length <= ChunkSize)
                    {
                        chunks.Add(text);
                        _logger.LogInformation("Text is smaller than chunk size. Returning 1 chunk of {Length} characters", text.Length);
                        return chunks;
                    }

                    // Chunking loop
                    while (currentIndex < text.Length)
                    {
                        // Calculate the end index for this chunk
                        var chunkEndIndex = currentIndex + ChunkSize;

                        // If we're at or past the end of text, grab remaining text
                        if (chunkEndIndex >= text.Length)
                        {
                            var finalChunk = text.Substring(currentIndex);
                            if (finalChunk.Length >= MinChunkSize)
                            {
                                chunks.Add(finalChunk);
                                _logger.LogInformation("Added final chunk (ChunkIndex: {Index}): {Length} characters",
                                    chunks.Count - 1, finalChunk.Length);
                            }
                            break;
                        }

                        // Find a good break point (whitespace) near the chunk boundary
                        // This avoids splitting words
                        var breakPoint = FindBreakPoint(text, currentIndex, chunkEndIndex);

                        // Extract chunk from currentIndex to breakPoint
                        var chunk = text.Substring(currentIndex, breakPoint - currentIndex);

                        chunks.Add(chunk);
                        _logger.LogInformation("Added chunk (ChunkIndex: {Index}): {Length} characters",
                            chunks.Count - 1, chunk.Length);

                        // Move to next chunk start position
                        // This is (breakPoint - overlap), but not before currentIndex
                        currentIndex = Math.Max(
                            breakPoint - OverlapSize,
                            currentIndex + MinChunkSize
                        );
                    }

                    _logger.LogInformation("Chunking complete. Generated {ChunkCount} chunks from {TextLength} characters",
                        chunks.Count, text.Length);

                    return chunks;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error chunking text");
                    throw;
                }
            });
        }
        private int FindBreakPoint(string text, int startIndex, int targetEndIndex)
        {
            // Search range: within ±50 characters of target
            // This allows some flexibility while staying close to ChunkSize
            var searchStart = Math.Max(startIndex, targetEndIndex - 50);
            var searchEnd = Math.Min(text.Length, targetEndIndex + 50);

            // Priority 1: Find line break (\n)
            for (int i = targetEndIndex - 1; i >= searchStart; i--)
            {
                if (text[i] == '\n')
                {
                    // Move past the newline
                    return i + 1;
                }
            }

            // Priority 2: Find sentence end (period followed by space)
            for (int i = targetEndIndex - 1; i >= searchStart; i--)
            {
                if (text[i] == '.' && i + 1 < text.Length && text[i + 1] == ' ')
                {
                    // Move past the space
                    return i + 2;
                }
            }

            // Priority 3: Find whitespace (space, tab, etc.)
            for (int i = targetEndIndex; i >= searchStart; i--)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    // Move past the whitespace
                    return i + 1;
                }
            }

            // Fallback: Use exact target position if no better break point found
            _logger.LogWarning("No natural break point found near {TargetIndex}. Using exact position.", targetEndIndex);
            return targetEndIndex;
        }
    }
}
