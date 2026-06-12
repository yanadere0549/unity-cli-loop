using System.Threading;
using System.Threading.Tasks;
using UnityEditor;

namespace io.github.hatayama.uLoopMCP
{
    [McpTool(Description = "List, select, or add Game View resolutions, and report current Game View state")]
    public class ControlGameViewTool : AbstractUnityTool<ControlGameViewSchema, ControlGameViewResponse>
    {
        public override string ToolName => "control-game-view";

        protected override Task<ControlGameViewResponse> ExecuteAsync(
            ControlGameViewSchema parameters,
            CancellationToken cancellationToken)
        {
            string actionName = parameters.Action.ToString();

            // Validate bridge type availability first (affects all actions)
            string[] displayTexts = GameViewSizesBridge.GetDisplayTexts();
            int totalCount = GameViewSizesBridge.GetTotalCount();
            if (totalCount < 0)
            {
                return Task.FromResult(new ControlGameViewResponse
                {
                    Success = false,
                    Action = actionName,
                    Message = "GameView type not available in this Unity version"
                });
            }

            switch (parameters.Action)
            {
                case GameViewAction.GetState:
                case GameViewAction.ListSizes:
                    return ExecuteGetStateAsync(actionName, displayTexts);

                case GameViewAction.SelectSize:
                    return ExecuteSelectSizeAsync(parameters, actionName, displayTexts, totalCount);

                case GameViewAction.AddCustomSize:
                    return ExecuteAddCustomSizeAsync(parameters, actionName, displayTexts, totalCount);

                default:
                    return Task.FromResult(new ControlGameViewResponse
                    {
                        Success = false,
                        Action = actionName,
                        Message = $"Unknown action: {parameters.Action}"
                    });
            }
        }

        private Task<ControlGameViewResponse> ExecuteGetStateAsync(
            string actionName,
            string[] displayTexts)
        {
            EditorWindow gameView = GameViewSizesBridge.FindGameViewWindow();
            int currentIndex = gameView != null
                ? GameViewSizesBridge.GetSelectedSizeIndex(gameView)
                : -1;
            string currentLabel = (currentIndex >= 0 && currentIndex < displayTexts.Length)
                ? displayTexts[currentIndex]
                : null;

            return Task.FromResult(new ControlGameViewResponse
            {
                Success = true,
                Action = actionName,
                Message = "Game View state retrieved successfully",
                CurrentSizeIndex = currentIndex >= 0 ? (int?)currentIndex : null,
                CurrentSizeLabel = currentLabel,
                AllSizeLabels = displayTexts
            });
        }

        private Task<ControlGameViewResponse> ExecuteSelectSizeAsync(
            ControlGameViewSchema parameters,
            string actionName,
            string[] displayTexts,
            int totalCount)
        {
            // Precondition: Game View window must be open
            EditorWindow gameView = GameViewSizesBridge.FindGameViewWindow();
            if (gameView == null)
            {
                return Task.FromResult(new ControlGameViewResponse
                {
                    Success = false,
                    Action = actionName,
                    Message = "No Game View window is open. Open Window > General > Game"
                });
            }

            int targetIndex;

            if (parameters.SizeIndex >= 0)
            {
                // Index-based selection: validate range
                if (parameters.SizeIndex >= totalCount)
                {
                    return Task.FromResult(new ControlGameViewResponse
                    {
                        Success = false,
                        Action = actionName,
                        Message = $"SizeIndex {parameters.SizeIndex} out of range (0..{totalCount - 1})"
                    });
                }

                targetIndex = parameters.SizeIndex;
            }
            else
            {
                // Label-based selection: require non-empty label
                if (string.IsNullOrEmpty(parameters.SizeLabel))
                {
                    return Task.FromResult(new ControlGameViewResponse
                    {
                        Success = false,
                        Action = actionName,
                        Message = "Provide SizeIndex >= 0 or a non-empty SizeLabel"
                    });
                }

                int foundIndex = GameViewSizesBridge.FindSizeByLabel(parameters.SizeLabel);
                if (foundIndex < 0)
                {
                    return Task.FromResult(new ControlGameViewResponse
                    {
                        Success = false,
                        Action = actionName,
                        Message = $"No Game View size matching '{parameters.SizeLabel}'"
                    });
                }

                targetIndex = foundIndex;

                // Check for multiple matches; use first, note in Message if multiple found
                int matchCount = CountMatchesByLabel(displayTexts, parameters.SizeLabel);
                string message = matchCount > 1
                    ? $"Multiple sizes match '{parameters.SizeLabel}'; using first match at index {targetIndex}"
                    : $"Size '{displayTexts[targetIndex]}' selected at index {targetIndex}";

                GameViewSizesBridge.SetSelectedSizeIndex(gameView, targetIndex);

                return Task.FromResult(new ControlGameViewResponse
                {
                    Success = true,
                    Action = actionName,
                    Message = message,
                    CurrentSizeIndex = targetIndex,
                    CurrentSizeLabel = displayTexts[targetIndex],
                    AllSizeLabels = displayTexts
                });
            }

            // Index-based path
            GameViewSizesBridge.SetSelectedSizeIndex(gameView, targetIndex);

            return Task.FromResult(new ControlGameViewResponse
            {
                Success = true,
                Action = actionName,
                Message = $"Size '{displayTexts[targetIndex]}' selected at index {targetIndex}",
                CurrentSizeIndex = targetIndex,
                CurrentSizeLabel = displayTexts[targetIndex],
                AllSizeLabels = displayTexts
            });
        }

        private Task<ControlGameViewResponse> ExecuteAddCustomSizeAsync(
            ControlGameViewSchema parameters,
            string actionName,
            string[] displayTexts,
            int totalCount)
        {
            // Precondition: width [1, 7680]
            if (parameters.CustomWidth < 1 || parameters.CustomWidth > 7680)
            {
                return Task.FromResult(new ControlGameViewResponse
                {
                    Success = false,
                    Action = actionName,
                    Message = $"CustomWidth must be 1-7680, got: {parameters.CustomWidth}"
                });
            }

            // Precondition: height [1, 4320]
            if (parameters.CustomHeight < 1 || parameters.CustomHeight > 4320)
            {
                return Task.FromResult(new ControlGameViewResponse
                {
                    Success = false,
                    Action = actionName,
                    Message = $"CustomHeight must be 1-4320, got: {parameters.CustomHeight}"
                });
            }

            // Precondition: label must not be empty
            if (string.IsNullOrEmpty(parameters.CustomLabel))
            {
                return Task.FromResult(new ControlGameViewResponse
                {
                    Success = false,
                    Action = actionName,
                    Message = "CustomLabel must not be empty"
                });
            }

            // Precondition: check for duplicate label using substring match (same logic as SelectSize by label)
            int duplicateIndex = GameViewSizesBridge.FindSizeByLabel(parameters.CustomLabel);
            if (duplicateIndex >= 0)
            {
                return Task.FromResult(new ControlGameViewResponse
                {
                    Success = false,
                    Action = actionName,
                    Message = $"Custom size '{parameters.CustomLabel}' already exists at index {duplicateIndex}"
                });
            }

            int newIndex = GameViewSizesBridge.AddCustomSize(
                parameters.CustomWidth,
                parameters.CustomHeight,
                parameters.CustomLabel);

            if (newIndex < 0)
            {
                return Task.FromResult(new ControlGameViewResponse
                {
                    Success = false,
                    Action = actionName,
                    Message = "GameView type not available in this Unity version"
                });
            }

            // Refresh display texts after adding
            string[] updatedTexts = GameViewSizesBridge.GetDisplayTexts();

            return Task.FromResult(new ControlGameViewResponse
            {
                Success = true,
                Action = actionName,
                Message = $"Custom size '{parameters.CustomLabel}' ({parameters.CustomWidth}x{parameters.CustomHeight}) added at index {newIndex}",
                NewSizeIndex = newIndex,
                AllSizeLabels = updatedTexts
            });
        }

        // --- private helpers ---

        private static int CountMatchesByLabel(string[] texts, string labelSubstring)
        {
            int count = 0;
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].IndexOf(labelSubstring, System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    count++;
                }
            }
            return count;
        }

    }
}
