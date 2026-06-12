using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;

namespace io.github.hatayama.uLoopMCP
{
    [McpTool(Description = "Open, configure, and inspect the Unity Device Simulator (device, orientation, scale)")]
    public class ControlDeviceSimulatorTool : AbstractUnityTool<ControlDeviceSimulatorSchema, ControlDeviceSimulatorResponse>
    {
        public override string ToolName => "control-device-simulator";

        protected override async Task<ControlDeviceSimulatorResponse> ExecuteAsync(
            ControlDeviceSimulatorSchema parameters,
            CancellationToken cancellationToken)
        {
            // (1) Module availability check — precondition for all actions.
            if (!DeviceSimulatorBridge.IsModuleAvailable())
            {
                return new ControlDeviceSimulatorResponse
                {
                    Success = false,
                    Action = parameters.Action.ToString(),
                    Message = "Device Simulator module not available (UnityEditor.DeviceSimulatorModule not found). Requires Unity 2021.2 or later with Device Simulator built-in."
                };
            }

            // (2) Parameter validation — action-specific, before any window access.
            string validationError = ValidateActionParameters(parameters);
            if (validationError != null)
            {
                return new ControlDeviceSimulatorResponse
                {
                    Success = false,
                    Action = parameters.Action.ToString(),
                    Message = validationError
                };
            }

            // (3) Window resolution — open if needed or return error per AutoOpen rules.
            EditorWindow window = DeviceSimulatorBridge.FindSimulatorWindow();

            if (parameters.Action == DeviceSimulatorAction.Open)
            {
                // Open always calls ShowWindow regardless of current state.
                DeviceSimulatorBridge.OpenWindow();
                await EditorDelay.DelayFrame(2, cancellationToken);
                window = DeviceSimulatorBridge.FindSimulatorWindow();
            }
            else if (window == null)
            {
                if (!parameters.AutoOpen)
                {
                    return new ControlDeviceSimulatorResponse
                    {
                        Success = false,
                        Action = parameters.Action.ToString(),
                        IsWindowOpen = false,
                        Message = "Device Simulator window is not open. Set AutoOpen=true or call Action=Open first"
                    };
                }

                // AutoOpen=true: open the window and wait for initialization.
                DeviceSimulatorBridge.OpenWindow();
                await EditorDelay.DelayFrame(2, cancellationToken);
                window = DeviceSimulatorBridge.FindSimulatorWindow();
            }

            bool isWindowOpen = window != null;
            object main = isWindowOpen ? DeviceSimulatorBridge.GetMain(window) : null;

            // (4) Guard: window open but main not yet initialized — actions that require main
            //     would produce misleading errors without this check.
            if (isWindowOpen && main == null)
            {
                switch (parameters.Action)
                {
                    case DeviceSimulatorAction.GetState:
                    case DeviceSimulatorAction.ListDevices:
                    case DeviceSimulatorAction.SelectDevice:
                    case DeviceSimulatorAction.SetRotation:
                    case DeviceSimulatorAction.SetScale:
                        return new ControlDeviceSimulatorResponse
                        {
                            Success = false,
                            Action = parameters.Action.ToString(),
                            IsWindowOpen = true,
                            Message = "Device Simulator window opened but is not yet initialized. Retry shortly."
                        };
                }
            }

            // (5) Perform the action.
            switch (parameters.Action)
            {
                case DeviceSimulatorAction.Open:
                    return BuildStateResponse(parameters.Action, "Device Simulator window opened", window, main, includeDeviceList: true);

                case DeviceSimulatorAction.GetState:
                    return BuildStateResponse(parameters.Action, "Device Simulator state retrieved", window, main, includeDeviceList: true);

                case DeviceSimulatorAction.ListDevices:
                    return BuildStateResponse(parameters.Action, "Device list retrieved", window, main, includeDeviceList: true);

                case DeviceSimulatorAction.SelectDevice:
                    return ExecuteSelectDevice(parameters, window, main);

                case DeviceSimulatorAction.SetRotation:
                    return ExecuteSetRotation(parameters, window, main);

                case DeviceSimulatorAction.SetScale:
                    return ExecuteSetScale(parameters, window, main);

                default:
                    return new ControlDeviceSimulatorResponse
                    {
                        Success = false,
                        Action = parameters.Action.ToString(),
                        Message = $"Unknown action: {parameters.Action}"
                    };
            }
        }

        // Returns null when valid, or the error message string when invalid.
        private string ValidateActionParameters(ControlDeviceSimulatorSchema parameters)
        {
            switch (parameters.Action)
            {
                case DeviceSimulatorAction.SetRotation:
                    int r = parameters.Rotation;
                    if (r != 0 && r != 90 && r != 180 && r != 270)
                    {
                        return $"Rotation must be 0, 90, 180, or 270, got: {r}";
                    }
                    break;

                case DeviceSimulatorAction.SetScale:
                    int s = parameters.Scale;
                    if (s < 10 || s > 100)
                    {
                        return $"Scale must be 10-100, got: {s}";
                    }
                    break;

                case DeviceSimulatorAction.SelectDevice:
                    if (parameters.DeviceIndex < 0 && string.IsNullOrEmpty(parameters.DeviceName))
                    {
                        return "Provide DeviceIndex >= 0 or a non-empty DeviceName";
                    }
                    break;
            }

            return null;
        }

        private ControlDeviceSimulatorResponse ExecuteSelectDevice(
            ControlDeviceSimulatorSchema parameters,
            EditorWindow window,
            object main)
        {
            string[] deviceNames = main != null ? DeviceSimulatorBridge.GetDeviceNames(main) : Array.Empty<string>();
            int targetIndex;
            int nameMatchCount = 0;

            if (parameters.DeviceIndex >= 0)
            {
                // Index-based selection — validate bounds.
                if (parameters.DeviceIndex >= deviceNames.Length)
                {
                    return new ControlDeviceSimulatorResponse
                    {
                        Success = false,
                        Action = DeviceSimulatorAction.SelectDevice.ToString(),
                        IsWindowOpen = window != null,
                        Message = $"DeviceIndex {parameters.DeviceIndex} out of range (0..{deviceNames.Length - 1})"
                    };
                }

                targetIndex = parameters.DeviceIndex;
            }
            else
            {
                // Name-based substring search (case-insensitive).
                targetIndex = -1;
                for (int i = 0; i < deviceNames.Length; i++)
                {
                    if (deviceNames[i].IndexOf(parameters.DeviceName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        if (nameMatchCount == 0)
                        {
                            targetIndex = i;
                        }
                        nameMatchCount++;
                    }
                }

                if (targetIndex < 0)
                {
                    return new ControlDeviceSimulatorResponse
                    {
                        Success = false,
                        Action = DeviceSimulatorAction.SelectDevice.ToString(),
                        IsWindowOpen = window != null,
                        AllDeviceNames = deviceNames,
                        Message = $"No device matching '{parameters.DeviceName}'"
                    };
                }
            }

            DeviceSimulatorBridge.SetDeviceIndex(main, targetIndex);
            window?.Repaint();

            string message = $"Selected device index {targetIndex}: {deviceNames[targetIndex]}";
            if (parameters.DeviceIndex < 0 && nameMatchCount > 1)
            {
                message += $" (multiple matches found for '{parameters.DeviceName}', using first)";
            }

            return BuildStateResponse(DeviceSimulatorAction.SelectDevice, message, window, main, includeDeviceList: true);
        }

        private ControlDeviceSimulatorResponse ExecuteSetRotation(
            ControlDeviceSimulatorSchema parameters,
            EditorWindow window,
            object main)
        {
            DeviceSimulatorBridge.SetRotationDegrees(main, parameters.Rotation);
            window?.Repaint();

            return BuildStateResponse(
                DeviceSimulatorAction.SetRotation,
                $"Rotation set to {parameters.Rotation} degrees",
                window,
                main,
                includeDeviceList: false);
        }

        private ControlDeviceSimulatorResponse ExecuteSetScale(
            ControlDeviceSimulatorSchema parameters,
            EditorWindow window,
            object main)
        {
            DeviceSimulatorBridge.SetScale(main, parameters.Scale);
            window?.Repaint();

            return BuildStateResponse(
                DeviceSimulatorAction.SetScale,
                $"Scale set to {parameters.Scale}",
                window,
                main,
                includeDeviceList: false);
        }

        // (6) Build response with full state fields.
        private ControlDeviceSimulatorResponse BuildStateResponse(
            DeviceSimulatorAction action,
            string message,
            EditorWindow window,
            object main,
            bool includeDeviceList)
        {
            bool isOpen = window != null && main != null;

            ControlDeviceSimulatorResponse response = new ControlDeviceSimulatorResponse
            {
                Success = true,
                Action = action.ToString(),
                Message = message,
                IsWindowOpen = isOpen
            };

            if (!isOpen)
            {
                return response;
            }

            response.CurrentDeviceIndex = DeviceSimulatorBridge.GetDeviceIndex(main);
            int rotationDegrees = DeviceSimulatorBridge.GetRotationDegrees(main);
            response.CurrentRotation = rotationDegrees >= 0 ? rotationDegrees : (int?)null;
            response.CurrentScale = DeviceSimulatorBridge.GetScale(main);
            response.ScreenWidth = DeviceSimulatorBridge.GetScreenWidth(main);
            response.ScreenHeight = DeviceSimulatorBridge.GetScreenHeight(main);
            response.OrientationName = DeviceSimulatorBridge.GetOrientationName(main);

            string[] deviceNames = DeviceSimulatorBridge.GetDeviceNames(main);
            int deviceIdx = response.CurrentDeviceIndex ?? -1;
            response.CurrentDeviceName = (deviceIdx >= 0 && deviceIdx < deviceNames.Length)
                ? deviceNames[deviceIdx]
                : null;

            if (includeDeviceList)
            {
                response.AllDeviceNames = deviceNames;
            }

            return response;
        }
    }
}
