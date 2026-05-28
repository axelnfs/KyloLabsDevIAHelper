// Core/Models/ResponseIA.cs
namespace KyloLabs.DevIAHelper.Core.Models
{
    public class ResponseIA
    {
        public string TraceId { get; set; } = Guid.NewGuid().ToString();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> DebugData { get; set; } = new();
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
