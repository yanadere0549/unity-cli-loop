using System.ComponentModel;

namespace io.github.hatayama.uLoopMCP
{
    public enum PlayModeViewType
    {
        GameView = 0,
        Simulator = 1
    }

    public class SwapPlayModeViewSchema : BaseToolSchema
    {
        [Description("Target play mode view type to make primary: GameView(0) - activate Game View, Simulator(1) - activate Device Simulator")]
        public PlayModeViewType TargetView { get; set; } = PlayModeViewType.GameView;
    }
}
