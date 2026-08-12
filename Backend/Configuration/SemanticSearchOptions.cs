namespace Backend.Configuration
{
    public class SemanticSearchOptions
    {
        public int DefaultTopK { get; set; } = 5; // Default number of top results to return
        public float DefaultSimilarityThreshold { get; set; } = 0.3f; // Default similarity threshold for filtering results
    }
}
