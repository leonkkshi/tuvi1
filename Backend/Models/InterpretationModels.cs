namespace Backend.Models
{
    // Request để yêu cầu AI giải đoán lá số
    public class InterpretationRequest
    {
        public TuViChart Chart { get; set; } = null!;
        public string FocusArea { get; set; } = "general"; // general, career, love, health, wealth
    }

    // Response từ AI
    public class InterpretationResponse
    {
        public string OverallInterpretation { get; set; } = string.Empty;
        public List<PalaceInterpretation> PalaceInterpretations { get; set; } = new();
        public List<string> KeyInsights { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }

    // Giải đoán chi tiết cho từng cung
    public class PalaceInterpretation
    {
        public string PalaceName { get; set; } = string.Empty;
        public string Interpretation { get; set; } = string.Empty;
        public List<string> InfluencingStars { get; set; } = new();
    }

    // Queue statistics
    public class QueueStats
    {
        public int TotalQueued { get; set; }
        public int TotalProcessing { get; set; }
        public int ChartQueueSize { get; set; }
        public int PalaceQueueSize { get; set; }
        public int TotalQueueSize { get; set; }
    }
}
