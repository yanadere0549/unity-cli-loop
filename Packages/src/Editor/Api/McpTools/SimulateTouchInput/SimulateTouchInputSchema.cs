using System.ComponentModel;

namespace io.github.hatayama.uLoopMCP
{
    /// <summary>
    /// Defines the type of touch gesture to simulate.
    /// </summary>
    public enum TouchAction
    {
        Tap = 0,       // Began+Ended at (X,Y); optional Duration hold
        LongPress = 1, // Began at (X,Y), hold Duration seconds, Ended
        Drag = 2       // Began at (FromX,FromY), Moved to (X,Y), Ended
    }

    /// <summary>
    /// Specifies which view's coordinate system is used for the touch coordinates.
    /// </summary>
    public enum TouchTargetView
    {
        GameView = 0,  // Coordinates in Game View target resolution space
        Simulator = 1  // Coordinates in Device Simulator screen space
    }

    /// <summary>
    /// Schema for the simulate-touch-input tool.
    /// </summary>
    public class SimulateTouchInputSchema : BaseToolSchema
    {
        [Description("Touch action: Tap(0) - quick touch at (X,Y), LongPress(1) - touch and hold at (X,Y) for Duration seconds, Drag(2) - touch move from (FromX,FromY) to (X,Y)")]
        public TouchAction Action { get; set; } = TouchAction.Tap;

        [Description("Target X in SimX coordinates (top-left origin, pixels at the target resolution). For Drag, this is the endpoint X.")]
        public float X { get; set; } = 0f;

        [Description("Target Y in SimY coordinates (top-left origin, pixels at the target resolution). For Drag, this is the endpoint Y.")]
        public float Y { get; set; } = 0f;

        [Description("Start X for Drag action (top-left origin).")]
        public float FromX { get; set; } = 0f;

        [Description("Start Y for Drag action (top-left origin).")]
        public float FromY { get; set; } = 0f;

        [Description("Touch identifier (0-9). Use distinct values for concurrent touches. Defaults to 0.")]
        public int TouchId { get; set; } = 0;

        [Description("Hold duration in seconds for LongPress (must be > 0). Minimum hold time for Tap (0 = one-shot).")]
        public float Duration { get; set; } = 0f;

        [Description("Drag interpolation speed in pixels per second (0 = instant single move event). Used by Drag action.")]
        public float DragSpeed { get; set; } = 500f;

        [Description("Target view for coordinate interpretation: GameView(0) - coordinates in Game View target resolution space, Simulator(1) - coordinates in Device Simulator screen space. Default: GameView.")]
        public TouchTargetView TargetView { get; set; } = TouchTargetView.GameView;
    }
}
