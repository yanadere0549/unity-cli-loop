#nullable enable

namespace io.github.hatayama.uLoopMCP
{
    public class SwapPlayModeViewResponse : BaseToolResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string? ActiveViewType { get; set; }   // "GameView" or "Simulator" after swap
    }
}
