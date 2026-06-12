#nullable enable

namespace io.github.hatayama.uLoopMCP
{
    public class ControlGameViewResponse : BaseToolResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public string Action { get; set; } = "";
        public int? CurrentSizeIndex { get; set; }
        public string? CurrentSizeLabel { get; set; }
        public string[]? AllSizeLabels { get; set; }  // GetState/ListSizes/SelectSize
        public int? NewSizeIndex { get; set; }         // AddCustomSize: index of added entry
    }
}
