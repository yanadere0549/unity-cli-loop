#if ULOOPMCP_HAS_INPUT_SYSTEM
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace io.github.hatayama.uLoopMCP
{
    /// <summary>
    /// Bridge class for managing the simulated Touchscreen device used by SimulateTouchInput.
    /// Creates and removes a dedicated Touchscreen device named "uLoopMCP Simulated Touchscreen".
    /// The device is removed automatically when PlayMode exits.
    /// </summary>
    [InitializeOnLoad]
    public static class SimulateTouchscreenBridge
    {
        private const string DeviceName = "uLoopMCP Simulated Touchscreen";

        // Stored device reference — never use Touchscreen.current, which may point to
        // the Device Simulator's own "Simulated Touchscreen" or a physical device.
        private static Touchscreen _simulatedDevice;

        static SimulateTouchscreenBridge()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        /// <summary>
        /// Returns the existing simulated Touchscreen device, creating it if necessary.
        /// Returns null if device creation fails.
        /// </summary>
        public static Touchscreen EnsureDevice()
        {
            if (_simulatedDevice != null)
            {
                return _simulatedDevice;
            }

            _simulatedDevice = InputSystem.AddDevice<Touchscreen>(DeviceName);
            return _simulatedDevice;
        }

        /// <summary>
        /// Removes the simulated Touchscreen device from the Input System.
        /// No-op if the device was not created or has already been removed.
        /// </summary>
        public static void RemoveDevice()
        {
            if (_simulatedDevice == null)
            {
                return;
            }

            InputSystem.RemoveDevice(_simulatedDevice);
            _simulatedDevice = null;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                RemoveDevice();
            }
        }
    }
}
#endif
