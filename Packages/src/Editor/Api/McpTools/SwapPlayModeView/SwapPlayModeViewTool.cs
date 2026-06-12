using System.Threading;
using System.Threading.Tasks;
using UnityEditor;

namespace io.github.hatayama.uLoopMCP
{
    [McpTool(Description = "Switch the primary play mode view between Game View and Device Simulator")]
    public class SwapPlayModeViewTool : AbstractUnityTool<SwapPlayModeViewSchema, SwapPlayModeViewResponse>
    {
        public override string ToolName => "swap-play-mode-view";

        protected override Task<SwapPlayModeViewResponse> ExecuteAsync(
            SwapPlayModeViewSchema parameters,
            CancellationToken cancellationToken)
        {
            // Precondition: Simulator target requires the module to be available
            if (parameters.TargetView == PlayModeViewType.Simulator
                && !DeviceSimulatorBridge.IsModuleAvailable())
            {
                return Task.FromResult(new SwapPlayModeViewResponse
                {
                    Success = false,
                    Message = "Device Simulator module not available"
                });
            }

            // Precondition: a PlayModeView window must be open
            EditorWindow currentView = PlayModeViewBridge.GetMainPlayModeView();
            if (currentView == null)
            {
                return Task.FromResult(new SwapPlayModeViewResponse
                {
                    Success = false,
                    Message = "No PlayModeView window is open"
                });
            }

            // Check current type to detect no-op
            string currentTypeName = PlayModeViewBridge.GetCurrentViewTypeName();
            string targetTypeName = parameters.TargetView == PlayModeViewType.GameView
                ? "GameView"
                : "Simulator";

            if (currentTypeName == targetTypeName)
            {
                return Task.FromResult(new SwapPlayModeViewResponse
                {
                    Success = true,
                    Message = $"Already active: {targetTypeName}",
                    ActiveViewType = currentTypeName
                });
            }

            // Perform the swap
            if (parameters.TargetView == PlayModeViewType.GameView)
            {
                PlayModeViewBridge.SwapToGameView();
            }
            else
            {
                PlayModeViewBridge.SwapToSimulator();
            }

            string newTypeName = PlayModeViewBridge.GetCurrentViewTypeName() ?? targetTypeName;

            return Task.FromResult(new SwapPlayModeViewResponse
            {
                Success = true,
                Message = $"Swapped play mode view to {newTypeName}",
                ActiveViewType = newTypeName
            });
        }
    }
}
