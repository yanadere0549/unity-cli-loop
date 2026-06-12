using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace io.github.hatayama.uLoopMCP
{
    /// <summary>
    /// Bridge class for accessing Unity Device Simulator internal APIs via reflection.
    /// The Device Simulator lives in the UnityEditor.DeviceSimulatorModule assembly (Unity 6000.0+)
    /// or in the UnityEditor assembly (2022.3 fallback).
    /// </summary>
    public static class DeviceSimulatorBridge
    {
        // Module availability
        private static bool _moduleAvailable;
        private static bool _memberSearchDone;

        // SimulatorWindow
        private static Type _simulatorWindowType;
        private static MethodInfo _showWindowMethod;
        private static PropertyInfo _mainProp;

        // DeviceSimulatorMain
        private static Type _deviceSimulatorMainType;
        private static PropertyInfo _devicesProp;
        private static PropertyInfo _deviceIndexProp;
        private static PropertyInfo _userInterfaceProp;
        private static PropertyInfo _screenSimulationProp;

        // DeviceInfoAsset / DeviceInfo
        private static FieldInfo _deviceInfoField;
        private static FieldInfo _friendlyNameField;

        // ScreenSimulation
        private static Type _screenSimulationType;
        private static PropertyInfo _deviceRotationProp;
        private static MethodInfo _applyChangesMethod;
        private static PropertyInfo _orientationProp;
        private static PropertyInfo _screenWidthProp;
        private static PropertyInfo _screenHeightProp;

        // UserInterfaceController
        private static Type _userInterfaceControllerType;
        private static PropertyInfo _scaleProp;
        private static PropertyInfo _rotationProp;

        /// <summary>
        /// Returns true if the Device Simulator module could be resolved and all required types were found.
        /// </summary>
        public static bool IsModuleAvailable()
        {
            EnsureMembersResolved();
            return _moduleAvailable;
        }

        /// <summary>
        /// Opens (or brings to front) the Device Simulator window.
        /// No-op if the module is not available.
        /// </summary>
        public static void OpenWindow()
        {
            EnsureMembersResolved();

            if (!_moduleAvailable || _showWindowMethod == null)
            {
                return;
            }

            _showWindowMethod.Invoke(null, null);
        }

        /// <summary>
        /// Finds an open SimulatorWindow EditorWindow instance, or null if none is open.
        /// </summary>
        public static EditorWindow FindSimulatorWindow()
        {
            EnsureMembersResolved();

            if (!_moduleAvailable || _simulatorWindowType == null)
            {
                return null;
            }

            UnityEngine.Object[] windows = Resources.FindObjectsOfTypeAll(_simulatorWindowType);
            return windows.Length > 0 ? windows[0] as EditorWindow : null;
        }

        /// <summary>
        /// Returns the DeviceSimulatorMain instance from a SimulatorWindow EditorWindow.
        /// Returns null if the window is null or the property is not available.
        /// </summary>
        public static object GetMain(EditorWindow window)
        {
            EnsureMembersResolved();

            if (!_moduleAvailable || window == null || _mainProp == null)
            {
                return null;
            }

            return _mainProp.GetValue(window);
        }

        /// <summary>
        /// Returns an array of friendly device names from the DeviceSimulatorMain instance.
        /// Returns an empty array if not available.
        /// </summary>
        public static string[] GetDeviceNames(object main)
        {
            EnsureMembersResolved();

            if (!_moduleAvailable || main == null || _devicesProp == null
                || _deviceInfoField == null || _friendlyNameField == null)
            {
                return Array.Empty<string>();
            }

            Array devices = _devicesProp.GetValue(main) as Array;
            if (devices == null)
            {
                return Array.Empty<string>();
            }

            string[] names = new string[devices.Length];
            for (int i = 0; i < devices.Length; i++)
            {
                object asset = devices.GetValue(i);
                if (asset == null)
                {
                    names[i] = string.Empty;
                    continue;
                }

                object info = _deviceInfoField.GetValue(asset);
                if (info == null)
                {
                    names[i] = string.Empty;
                    continue;
                }

                names[i] = _friendlyNameField.GetValue(info) as string ?? string.Empty;
            }

            return names;
        }

        /// <summary>
        /// Returns the currently selected device index from the DeviceSimulatorMain instance.
        /// Returns -1 if not available.
        /// </summary>
        public static int GetDeviceIndex(object main)
        {
            EnsureMembersResolved();

            if (!_moduleAvailable || main == null || _deviceIndexProp == null)
            {
                return -1;
            }

            object result = _deviceIndexProp.GetValue(main);
            return result is int i ? i : -1;
        }

        /// <summary>
        /// Sets the selected device index on the DeviceSimulatorMain instance.
        /// No-op if not available.
        /// </summary>
        public static void SetDeviceIndex(object main, int index)
        {
            EnsureMembersResolved();

            if (!_moduleAvailable || main == null || _deviceIndexProp == null)
            {
                return;
            }

            _deviceIndexProp.SetValue(main, index);
        }

        /// <summary>
        /// Returns the current scale percentage (10-100) from UserInterfaceController.
        /// Returns -1 if not available.
        /// </summary>
        public static int GetScale(object main)
        {
            EnsureMembersResolved();

            object ui = GetUserInterface(main);
            if (ui == null || _scaleProp == null)
            {
                return -1;
            }

            object result = _scaleProp.GetValue(ui);
            return result is int i ? i : -1;
        }

        /// <summary>
        /// Sets the scale percentage via UserInterfaceController.Scale property (triggers callbacks).
        /// No-op if not available.
        /// </summary>
        public static void SetScale(object main, int scale)
        {
            EnsureMembersResolved();

            object ui = GetUserInterface(main);
            if (ui == null || _scaleProp == null)
            {
                return;
            }

            _scaleProp.SetValue(ui, scale);
        }

        /// <summary>
        /// Returns the current rotation in degrees (0/90/180/270) derived from ScreenSimulation.orientation.
        /// UserInterfaceController.Rotation reflects the UI control state and stays 0 after SetRotationDegrees;
        /// ScreenSimulation.orientation correctly tracks the applied simulation rotation.
        /// Mapping (empirically confirmed on Unity 6000.0): Portrait→0, LandscapeRight→90,
        /// PortraitUpsideDown→180, LandscapeLeft→270.
        /// Returns -1 if not available or orientation is unknown.
        /// </summary>
        public static int GetRotationDegrees(object main)
        {
            EnsureMembersResolved();

            object screen = GetScreenSimulation(main);
            if (screen == null || _orientationProp == null)
            {
                return -1;
            }

            object result = _orientationProp.GetValue(screen);
            if (result == null)
            {
                return -1;
            }

            switch (result.ToString())
            {
                case "Portrait":          return 0;
                case "LandscapeRight":    return 90;
                case "PortraitUpsideDown": return 180;
                case "LandscapeLeft":     return 270;
                default:                  return -1;
            }
        }

        /// <summary>
        /// Sets the rotation in degrees via ScreenSimulation.DeviceRotation + ApplyChanges().
        /// No-op if not available.
        /// </summary>
        public static void SetRotationDegrees(object main, int degrees)
        {
            EnsureMembersResolved();

            object screen = GetScreenSimulation(main);
            if (screen == null || _deviceRotationProp == null || _applyChangesMethod == null)
            {
                return;
            }

            _deviceRotationProp.SetValue(screen, degrees);
            _applyChangesMethod.Invoke(screen, null);
        }

        /// <summary>
        /// Returns the simulated screen width in pixels.
        /// Returns -1 if not available.
        /// </summary>
        public static int GetScreenWidth(object main)
        {
            EnsureMembersResolved();

            object screen = GetScreenSimulation(main);
            if (screen == null || _screenWidthProp == null)
            {
                return -1;
            }

            object result = _screenWidthProp.GetValue(screen);
            return result is int i ? i : -1;
        }

        /// <summary>
        /// Returns the simulated screen height in pixels.
        /// Returns -1 if not available.
        /// </summary>
        public static int GetScreenHeight(object main)
        {
            EnsureMembersResolved();

            object screen = GetScreenSimulation(main);
            if (screen == null || _screenHeightProp == null)
            {
                return -1;
            }

            object result = _screenHeightProp.GetValue(screen);
            return result is int i ? i : -1;
        }

        /// <summary>
        /// Returns the current orientation name (e.g. "Portrait", "LandscapeLeft").
        /// Returns an empty string if not available.
        /// </summary>
        public static string GetOrientationName(object main)
        {
            EnsureMembersResolved();

            object screen = GetScreenSimulation(main);
            if (screen == null || _orientationProp == null)
            {
                return string.Empty;
            }

            object result = _orientationProp.GetValue(screen);
            return result != null ? result.ToString() : string.Empty;
        }

        // --- private helpers ---

        private static object GetUserInterface(object main)
        {
            if (!_moduleAvailable || main == null || _userInterfaceProp == null)
            {
                return null;
            }

            return _userInterfaceProp.GetValue(main);
        }

        private static object GetScreenSimulation(object main)
        {
            if (!_moduleAvailable || main == null || _screenSimulationProp == null)
            {
                return null;
            }

            return _screenSimulationProp.GetValue(main);
        }

        private static System.Reflection.Assembly ResolveDeviceSimulatorAssembly()
        {
            // Unity 6000.0+: Device Simulator lives in its own module assembly
            System.Reflection.Assembly modAsm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "UnityEditor.DeviceSimulatorModule");
            if (modAsm != null)
            {
                return modAsm;
            }

            // 2022.3 fallback: types were in the main UnityEditor assembly
            return typeof(EditorWindow).Assembly;
        }

        private static void EnsureMembersResolved()
        {
            if (_memberSearchDone)
            {
                return;
            }
            _memberSearchDone = true;

            System.Reflection.Assembly dsAsm = ResolveDeviceSimulatorAssembly();

            // --- SimulatorWindow ---
            _simulatorWindowType = dsAsm.GetType("UnityEditor.DeviceSimulation.SimulatorWindow");
            if (_simulatorWindowType == null)
            {
                Debug.LogWarning("[DeviceSimulatorBridge] SimulatorWindow type not found — module unavailable");
                return;
            }

            _showWindowMethod = _simulatorWindowType.GetMethod(
                "ShowWindow",
                BindingFlags.Public | BindingFlags.Static);
            if (_showWindowMethod == null)
            {
                Debug.LogWarning("[DeviceSimulatorBridge] ShowWindow method not found");
            }

            _mainProp = _simulatorWindowType.GetProperty(
                "main",
                BindingFlags.Public | BindingFlags.Instance);
            if (_mainProp == null)
            {
                Debug.LogWarning("[DeviceSimulatorBridge] main property not found");
            }

            // --- DeviceSimulatorMain ---
            _deviceSimulatorMainType = dsAsm.GetType("UnityEditor.DeviceSimulation.DeviceSimulatorMain");
            if (_deviceSimulatorMainType == null)
            {
                Debug.LogWarning("[DeviceSimulatorBridge] DeviceSimulatorMain type not found");
                return;
            }

            _devicesProp = _deviceSimulatorMainType.GetProperty(
                "devices",
                BindingFlags.Public | BindingFlags.Instance);
            if (_devicesProp == null)
            {
                Debug.LogWarning("[DeviceSimulatorBridge] devices property not found");
            }

            _deviceIndexProp = _deviceSimulatorMainType.GetProperty(
                "deviceIndex",
                BindingFlags.Public | BindingFlags.Instance);
            if (_deviceIndexProp == null)
            {
                Debug.LogWarning("[DeviceSimulatorBridge] deviceIndex property not found");
            }

            _userInterfaceProp = _deviceSimulatorMainType.GetProperty(
                "userInterface",
                BindingFlags.Public | BindingFlags.Instance);
            if (_userInterfaceProp == null)
            {
                Debug.LogWarning("[DeviceSimulatorBridge] userInterface property not found");
            }

            _screenSimulationProp = _deviceSimulatorMainType.GetProperty(
                "ScreenSimulation",
                BindingFlags.Public | BindingFlags.Instance);
            if (_screenSimulationProp == null)
            {
                Debug.LogWarning("[DeviceSimulatorBridge] ScreenSimulation property not found");
            }

            // --- DeviceInfoAsset / DeviceInfo ---
            Type deviceInfoAssetType = dsAsm.GetType("UnityEditor.DeviceSimulation.DeviceInfoAsset");
            if (deviceInfoAssetType == null)
            {
                Debug.LogWarning("[DeviceSimulatorBridge] DeviceInfoAsset type not found");
                return;
            }

            _deviceInfoField = deviceInfoAssetType.GetField(
                "deviceInfo",
                BindingFlags.Public | BindingFlags.Instance);
            if (_deviceInfoField == null)
            {
                Debug.LogWarning("[DeviceSimulatorBridge] deviceInfo field not found");
            }

            if (_deviceInfoField != null)
            {
                Type deviceInfoType = _deviceInfoField.FieldType;
                _friendlyNameField = deviceInfoType.GetField(
                    "friendlyName",
                    BindingFlags.Public | BindingFlags.Instance);
                if (_friendlyNameField == null)
                {
                    Debug.LogWarning("[DeviceSimulatorBridge] friendlyName field not found");
                }
            }

            // --- ScreenSimulation ---
            _screenSimulationType = dsAsm.GetType("UnityEditor.DeviceSimulation.ScreenSimulation");
            if (_screenSimulationType == null)
            {
                Debug.LogWarning("[DeviceSimulatorBridge] ScreenSimulation type not found");
                return;
            }

            // DeviceRotation is a set-only property
            _deviceRotationProp = _screenSimulationType.GetProperty(
                "DeviceRotation",
                BindingFlags.Public | BindingFlags.Instance);
            if (_deviceRotationProp == null)
            {
                Debug.LogWarning("[DeviceSimulatorBridge] DeviceRotation property not found");
            }

            _applyChangesMethod = _screenSimulationType.GetMethod(
                "ApplyChanges",
                BindingFlags.Public | BindingFlags.Instance);
            if (_applyChangesMethod == null)
            {
                Debug.LogWarning("[DeviceSimulatorBridge] ApplyChanges method not found");
            }

            _orientationProp = _screenSimulationType.GetProperty(
                "orientation",
                BindingFlags.Public | BindingFlags.Instance);
            if (_orientationProp == null)
            {
                Debug.LogWarning("[DeviceSimulatorBridge] orientation property not found");
            }

            _screenWidthProp = _screenSimulationType.GetProperty(
                "width",
                BindingFlags.Public | BindingFlags.Instance);
            if (_screenWidthProp == null)
            {
                Debug.LogWarning("[DeviceSimulatorBridge] width property not found");
            }

            _screenHeightProp = _screenSimulationType.GetProperty(
                "height",
                BindingFlags.Public | BindingFlags.Instance);
            if (_screenHeightProp == null)
            {
                Debug.LogWarning("[DeviceSimulatorBridge] height property not found");
            }

            // --- UserInterfaceController ---
            _userInterfaceControllerType = dsAsm.GetType(
                "UnityEditor.DeviceSimulation.UserInterfaceController");
            if (_userInterfaceControllerType == null)
            {
                Debug.LogWarning("[DeviceSimulatorBridge] UserInterfaceController type not found");
                return;
            }

            // Scale and Rotation are internal properties — use NonPublic binding
            _scaleProp = _userInterfaceControllerType.GetProperty(
                "Scale",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (_scaleProp == null)
            {
                Debug.LogWarning("[DeviceSimulatorBridge] Scale property not found");
            }

            _rotationProp = _userInterfaceControllerType.GetProperty(
                "Rotation",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (_rotationProp == null)
            {
                Debug.LogWarning("[DeviceSimulatorBridge] Rotation property not found");
            }

            // All critical types and members resolved
            _moduleAvailable = true;
        }
    }
}
