#nullable enable
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
#if ULOOPMCP_HAS_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
#endif

namespace io.github.hatayama.uLoopMCP
{
    [McpTool(Description = "Simulate touch input (tap, long-press, drag) in PlayMode via Input System. Injects TouchState events into a dedicated Touchscreen device for game logic that reads Input System touch data. Requires the Input System package.")]
    public class SimulateTouchInputTool : AbstractUnityTool<SimulateTouchInputSchema, SimulateTouchInputResponse>
    {
        public override string ToolName => "simulate-touch-input";

        protected override
#if !ULOOPMCP_HAS_INPUT_SYSTEM
#pragma warning disable CS1998
#endif
            async Task<SimulateTouchInputResponse> ExecuteAsync(
            SimulateTouchInputSchema parameters,
            CancellationToken ct)
#if !ULOOPMCP_HAS_INPUT_SYSTEM
#pragma warning restore CS1998
#endif
        {
            ct.ThrowIfCancellationRequested();

#if !ULOOPMCP_HAS_INPUT_SYSTEM
            return new SimulateTouchInputResponse
            {
                Success = false,
                Message = "simulate-touch-input requires the Input System package",
                Action = parameters.Action.ToString()
            };
#else
            // --- PlayMode guards ---
            if (!EditorApplication.isPlaying)
            {
                return new SimulateTouchInputResponse
                {
                    Success = false,
                    Message = "PlayMode is not active. Use control-play-mode first.",
                    Action = parameters.Action.ToString()
                };
            }

            if (EditorApplication.isPaused)
            {
                return new SimulateTouchInputResponse
                {
                    Success = false,
                    Message = "PlayMode is paused. Resume PlayMode before simulating touch.",
                    Action = parameters.Action.ToString()
                };
            }

            // --- Parameter validation (DbC: fail fast at the boundary) ---
            if (parameters.TouchId < 0 || parameters.TouchId > 9)
            {
                return new SimulateTouchInputResponse
                {
                    Success = false,
                    Message = $"TouchId must be 0-9, got: {parameters.TouchId}",
                    Action = parameters.Action.ToString()
                };
            }

            if (parameters.Action == TouchAction.LongPress && parameters.Duration <= 0f)
            {
                return new SimulateTouchInputResponse
                {
                    Success = false,
                    Message = $"Duration must be positive for LongPress, got: {parameters.Duration}",
                    Action = parameters.Action.ToString()
                };
            }

            if (parameters.DragSpeed < 0f)
            {
                return new SimulateTouchInputResponse
                {
                    Success = false,
                    Message = $"DragSpeed must be non-negative, got: {parameters.DragSpeed}",
                    Action = parameters.Action.ToString()
                };
            }

            // --- Device setup ---
            Touchscreen? device = SimulateTouchscreenBridge.EnsureDevice();
            if (device == null)
            {
                return new SimulateTouchInputResponse
                {
                    Success = false,
                    Message = "Failed to obtain simulated Touchscreen device",
                    Action = parameters.Action.ToString()
                };
            }

            string correlationId = McpConstants.GenerateCorrelationId();

            VibeLogger.LogInfo(
                "simulate_touch_input_start",
                "Touch input simulation started",
                new { Action = parameters.Action.ToString(), TouchId = parameters.TouchId },
                correlationId: correlationId
            );

            SimulateTouchInputResponse response;

            switch (parameters.Action)
            {
                case TouchAction.Tap:
                    response = await ExecuteTap(device, parameters, ct);
                    break;

                case TouchAction.LongPress:
                    response = await ExecuteLongPress(device, parameters, ct);
                    break;

                case TouchAction.Drag:
                    response = await ExecuteDrag(device, parameters, ct);
                    break;

                default:
                    response = new SimulateTouchInputResponse
                    {
                        Success = false,
                        Message = $"Unknown touch action: {parameters.Action}",
                        Action = parameters.Action.ToString()
                    };
                    break;
            }

            VibeLogger.LogInfo(
                "simulate_touch_input_complete",
                $"Touch input simulation completed: {response.Message}",
                new { Action = parameters.Action.ToString(), Success = response.Success },
                correlationId: correlationId
            );

            return response;
#endif
        }

#if ULOOPMCP_HAS_INPUT_SYSTEM
        // ---------------------------------------------------------------------------
        // Coordinate transform
        // ---------------------------------------------------------------------------

        // Converts top-left-origin simulator coordinates to Input System screen space
        // (bottom-left origin). Uses the configured target view to determine the height.
        private static Vector2 SimToScreen(float simX, float simY, TouchTargetView targetView)
        {
            float targetHeight = ResolveTargetHeight(targetView);
            return new Vector2(simX, targetHeight - simY);
        }

        // Returns the screen height for the given target view.
        // Simulator: uses DeviceSimulatorBridge.GetScreenHeight; falls back to GameView if unavailable.
        // GameView: uses Handles.GetMainGameViewSize().y.
        private static float ResolveTargetHeight(TouchTargetView targetView)
        {
            if (targetView == TouchTargetView.Simulator)
            {
                EditorWindow? simWindow = DeviceSimulatorBridge.FindSimulatorWindow();
                if (simWindow != null)
                {
                    object? main = DeviceSimulatorBridge.GetMain(simWindow);
                    if (main != null)
                    {
                        int h = DeviceSimulatorBridge.GetScreenHeight(main);
                        if (h > 0)
                        {
                            return (float)h;
                        }
                    }
                }
            }

            // GameView fallback (also the default path for TargetView=GameView)
            return Handles.GetMainGameViewSize().y;
        }

        // ---------------------------------------------------------------------------
        // TouchState helpers
        // ---------------------------------------------------------------------------

        // Input System requires touchId >= 1; touchId=0 is treated as invalid and causes
        // the Touchscreen to ignore the event entirely (see Touchscreen.cs source comment:
        // "Must have a valid, non-zero touch ID"). The user-facing TouchId parameter is
        // 0-9, so we map N -> N+1 to keep the API zero-based while satisfying the constraint.
        private static int ToInternalTouchId(int userTouchId)
        {
            return userTouchId + 1;
        }

        private static TouchState MakeState(int internalTouchId, TouchPhase phase, Vector2 screenPos)
        {
            return new TouchState
            {
                touchId = internalTouchId,
                phase = phase,
                position = screenPos
            };
        }

        private static void QueueState(Touchscreen device, TouchState state)
        {
            // Always pass the stored device reference explicitly — never rely on Touchscreen.current,
            // which may point to the Device Simulator's "Simulated Touchscreen" or a physical device.
            InputSystem.QueueStateEvent(device, state);
        }

        private static bool CanInjectTouchState(Touchscreen device)
        {
            return EditorApplication.isPlaying && device.added;
        }

        // ---------------------------------------------------------------------------
        // Tap
        // ---------------------------------------------------------------------------

        private async Task<SimulateTouchInputResponse> ExecuteTap(
            Touchscreen device, SimulateTouchInputSchema parameters, CancellationToken ct)
        {
            Vector2 inputPos = new Vector2(parameters.X, parameters.Y);
            Vector2 screenPos = SimToScreen(parameters.X, parameters.Y, parameters.TargetView);
            int internalId = ToInternalTouchId(parameters.TouchId);

            // Inject Began
            await InputSystemUpdateHelper.ApplyOnNextConfiguredUpdate(
                () => QueueState(device, MakeState(internalId, TouchPhase.Began, screenPos)), ct);

            bool endedInjected = false;
            try
            {
                if (parameters.Duration <= 0f)
                {
                    // One-shot tap: Began -> next frame -> Ended
                    await EditorDelay.DelayFrame(1, ct);
                }
                else
                {
                    // Tap with hold: Began -> Stationary each frame until Duration elapsed -> Ended
                    float startTime = Time.realtimeSinceStartup;
                    float elapsed = 0f;
                    while (elapsed < parameters.Duration)
                    {
                        await EditorDelay.DelayFrame(1, ct);
                        await InputSystemUpdateHelper.ApplyOnNextConfiguredUpdate(
                            () => QueueState(device, MakeState(internalId, TouchPhase.Stationary, screenPos)), ct);
                        elapsed = Time.realtimeSinceStartup - startTime;
                    }
                }

                // Inject Ended
                await InputSystemUpdateHelper.ApplyOnNextConfiguredUpdate(
                    () => QueueState(device, MakeState(internalId, TouchPhase.Ended, screenPos)), ct);
                endedInjected = true;

                // Wait observation frames so game code observes the release
                await InputSystemUpdateHelper.WaitForObservationFrames(ct);
            }
            finally
            {
                if (!endedInjected && CanInjectTouchState(device))
                {
                    InputSystem.QueueStateEvent(device, MakeState(internalId, TouchPhase.Ended, screenPos));
                }
            }

            string durationText = parameters.Duration > 0f ? $" for {parameters.Duration:F1}s" : "";
            return new SimulateTouchInputResponse
            {
                Success = true,
                Message = $"Tapped at ({inputPos.x:F1}, {inputPos.y:F1}){durationText} (TouchId={parameters.TouchId})",
                Action = TouchAction.Tap.ToString(),
                TouchId = parameters.TouchId,
                PositionX = inputPos.x,
                PositionY = inputPos.y
            };
        }

        // ---------------------------------------------------------------------------
        // LongPress
        // ---------------------------------------------------------------------------

        private async Task<SimulateTouchInputResponse> ExecuteLongPress(
            Touchscreen device, SimulateTouchInputSchema parameters, CancellationToken ct)
        {
            Vector2 inputPos = new Vector2(parameters.X, parameters.Y);
            Vector2 screenPos = SimToScreen(parameters.X, parameters.Y, parameters.TargetView);
            int internalId = ToInternalTouchId(parameters.TouchId);

            // Inject Began
            await InputSystemUpdateHelper.ApplyOnNextConfiguredUpdate(
                () => QueueState(device, MakeState(internalId, TouchPhase.Began, screenPos)), ct);

            bool endedInjected = false;
            try
            {
                // Stationary each frame until Duration elapsed
                float startTime = Time.realtimeSinceStartup;
                float elapsed = 0f;
                while (elapsed < parameters.Duration)
                {
                    await EditorDelay.DelayFrame(1, ct);
                    await InputSystemUpdateHelper.ApplyOnNextConfiguredUpdate(
                        () => QueueState(device, MakeState(internalId, TouchPhase.Stationary, screenPos)), ct);
                    elapsed = Time.realtimeSinceStartup - startTime;
                }

                // Inject Ended
                await InputSystemUpdateHelper.ApplyOnNextConfiguredUpdate(
                    () => QueueState(device, MakeState(internalId, TouchPhase.Ended, screenPos)), ct);
                endedInjected = true;

                // Wait observation frames so game code observes the release
                await InputSystemUpdateHelper.WaitForObservationFrames(ct);
            }
            finally
            {
                if (!endedInjected && CanInjectTouchState(device))
                {
                    InputSystem.QueueStateEvent(device, MakeState(internalId, TouchPhase.Ended, screenPos));
                }
            }

            return new SimulateTouchInputResponse
            {
                Success = true,
                Message = $"Long-pressed at ({inputPos.x:F1}, {inputPos.y:F1}) for {parameters.Duration:F1}s (TouchId={parameters.TouchId})",
                Action = TouchAction.LongPress.ToString(),
                TouchId = parameters.TouchId,
                PositionX = inputPos.x,
                PositionY = inputPos.y
            };
        }

        // ---------------------------------------------------------------------------
        // Drag
        // ---------------------------------------------------------------------------

        private async Task<SimulateTouchInputResponse> ExecuteDrag(
            Touchscreen device, SimulateTouchInputSchema parameters, CancellationToken ct)
        {
            Vector2 inputStart = new Vector2(parameters.FromX, parameters.FromY);
            Vector2 inputEnd = new Vector2(parameters.X, parameters.Y);
            Vector2 screenStart = SimToScreen(parameters.FromX, parameters.FromY, parameters.TargetView);
            Vector2 screenEnd = SimToScreen(parameters.X, parameters.Y, parameters.TargetView);
            int internalId = ToInternalTouchId(parameters.TouchId);

            // Inject Began at start position
            await InputSystemUpdateHelper.ApplyOnNextConfiguredUpdate(
                () => QueueState(device, MakeState(internalId, TouchPhase.Began, screenStart)), ct);

            bool endedInjected = false;
            try
            {
                if (parameters.DragSpeed <= 0f)
                {
                    // Instant: single Moved event at endpoint
                    await EditorDelay.DelayFrame(1, ct);
                    await InputSystemUpdateHelper.ApplyOnNextConfiguredUpdate(
                        () => QueueState(device, MakeState(internalId, TouchPhase.Moved, screenEnd)), ct);
                }
                else
                {
                    // Interpolated: per-frame Moved events at DragSpeed px/s
                    float distance = Vector2.Distance(screenStart, screenEnd);

                    // If start == end the drag has zero distance; fall through to a single Moved at the endpoint.
                    if (distance <= 0f)
                    {
                        await EditorDelay.DelayFrame(1, ct);
                        await InputSystemUpdateHelper.ApplyOnNextConfiguredUpdate(
                            () => QueueState(device, MakeState(internalId, TouchPhase.Moved, screenEnd)), ct);
                    }
                    else
                    {
                        float duration = distance / parameters.DragSpeed;
                        float startTime = Time.realtimeSinceStartup;
                        float t;

                        do
                        {
                            await EditorDelay.DelayFrame(1, ct);

                            float elapsed = Time.realtimeSinceStartup - startTime;
                            t = Mathf.Clamp01(elapsed / duration);
                            Vector2 currentPos = Vector2.Lerp(screenStart, screenEnd, t);

                            await InputSystemUpdateHelper.ApplyOnNextConfiguredUpdate(
                                () => QueueState(device, MakeState(internalId, TouchPhase.Moved, currentPos)), ct);
                        }
                        while (t < 1.0f);
                    }
                }

                // Inject Ended at endpoint
                await InputSystemUpdateHelper.ApplyOnNextConfiguredUpdate(
                    () => QueueState(device, MakeState(internalId, TouchPhase.Ended, screenEnd)), ct);
                endedInjected = true;

                // Wait observation frames so game code observes the release
                await InputSystemUpdateHelper.WaitForObservationFrames(ct);
            }
            finally
            {
                if (!endedInjected && CanInjectTouchState(device))
                {
                    InputSystem.QueueStateEvent(device, MakeState(internalId, TouchPhase.Ended, screenEnd));
                }
            }

            return new SimulateTouchInputResponse
            {
                Success = true,
                Message = $"Dragged from ({inputStart.x:F1}, {inputStart.y:F1}) to ({inputEnd.x:F1}, {inputEnd.y:F1}) at {parameters.DragSpeed:F0} px/s (TouchId={parameters.TouchId})",
                Action = TouchAction.Drag.ToString(),
                TouchId = parameters.TouchId,
                PositionX = inputStart.x,
                PositionY = inputStart.y,
                EndPositionX = inputEnd.x,
                EndPositionY = inputEnd.y
            };
        }
#endif
    }
}
