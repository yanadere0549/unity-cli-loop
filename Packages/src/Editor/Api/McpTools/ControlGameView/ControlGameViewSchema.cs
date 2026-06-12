using System.ComponentModel;

namespace io.github.hatayama.uLoopMCP
{
    public enum GameViewAction
    {
        GetState = 0,     // Report current index, all display texts, current size label
        ListSizes = 1,    // Alias for GetState (same data)
        SelectSize = 2,   // Set selectedSizeIndex by index or by label substring match
        AddCustomSize = 3 // Add FixedResolution entry with given W/H/Label
    }

    public class ControlGameViewSchema : BaseToolSchema
    {
        [Description("Action to perform: GetState(0)/ListSizes(1) - report current state and all available sizes, SelectSize(2) - activate a size by SizeIndex or SizeLabel, AddCustomSize(3) - add a new fixed-resolution entry")]
        public GameViewAction Action { get; set; } = GameViewAction.GetState;

        [Description("Zero-based index of the size to select (used by SelectSize). Takes priority over SizeLabel when both are provided.")]
        public int SizeIndex { get; set; } = -1;

        [Description("Label substring to match for SelectSize (case-insensitive). Used when SizeIndex is -1.")]
        public string SizeLabel { get; set; } = "";

        [Description("Width in pixels for AddCustomSize. Must be 1-7680.")]
        public int CustomWidth { get; set; } = 1920;

        [Description("Height in pixels for AddCustomSize. Must be 1-4320.")]
        public int CustomHeight { get; set; } = 1080;

        [Description("Label string for AddCustomSize. Must not be empty.")]
        public string CustomLabel { get; set; } = "";
    }
}
