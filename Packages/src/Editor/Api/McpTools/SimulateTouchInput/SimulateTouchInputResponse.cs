#nullable enable

namespace io.github.hatayama.uLoopMCP
{
    /// <summary>
    /// Response from the simulate-touch-input tool.
    /// </summary>
    public class SimulateTouchInputResponse : BaseToolResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string Action { get; set; } = "";
        public int TouchId { get; set; }
        public float? PositionX { get; set; }
        public float? PositionY { get; set; }
        public float? EndPositionX { get; set; }
        public float? EndPositionY { get; set; }
    }
}
