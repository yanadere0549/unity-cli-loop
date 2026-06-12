using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace io.github.hatayama.uLoopMCP
{
    /// <summary>
    /// Bridge class for accessing Unity PlayModeView internal APIs via reflection.
    /// Provides access to GetMainPlayModeView() and SwapMainWindow() for switching
    /// between Game View and Device Simulator as the primary play mode view.
    /// </summary>
    public static class PlayModeViewBridge
    {
        private static Type _playModeViewType;
        private static MethodInfo _getMainPlayModeViewMethod;
        private static MethodInfo _swapMainWindowMethod;
        private static Type _gameViewType;
        private static bool _memberSearchDone;

        /// <summary>
        /// Returns the current main PlayModeView EditorWindow, or null if none is open.
        /// </summary>
        public static EditorWindow GetMainPlayModeView()
        {
            EnsureMembersResolved();

            if (_getMainPlayModeViewMethod == null)
            {
                return null;
            }

            return _getMainPlayModeViewMethod.Invoke(null, null) as EditorWindow;
        }

        /// <summary>
        /// Swaps the main play mode view to the Game View type.
        /// No-op if the current view is null or the method is not available.
        /// </summary>
        public static void SwapToGameView()
        {
            EnsureMembersResolved();

            if (_gameViewType == null)
            {
                return;
            }

            SwapTo(_gameViewType);
        }

        /// <summary>
        /// Swaps the main play mode view to the Device Simulator (SimulatorWindow) type.
        /// No-op if the simulator type is not available or the method is not available.
        /// </summary>
        public static void SwapToSimulator()
        {
            EnsureMembersResolved();

            Type simulatorType = ResolveSimulatorWindowType();
            if (simulatorType == null)
            {
                return;
            }

            SwapTo(simulatorType);
        }

        /// <summary>
        /// Returns the type name of the current main play mode view:
        /// "GameView", "Simulator", or "Unknown" (or null if no view is open).
        /// </summary>
        public static string GetCurrentViewTypeName()
        {
            EnsureMembersResolved();

            EditorWindow view = GetMainPlayModeView();
            if (view == null)
            {
                return null;
            }

            Type viewType = view.GetType();

            if (_gameViewType != null && viewType == _gameViewType)
            {
                return "GameView";
            }

            Type simulatorType = ResolveSimulatorWindowType();
            if (simulatorType != null && viewType == simulatorType)
            {
                return "Simulator";
            }

            return "Unknown";
        }

        // --- private helpers ---

        private static void SwapTo(Type targetType)
        {
            EditorWindow view = GetMainPlayModeView();
            if (view == null || _swapMainWindowMethod == null)
            {
                return;
            }

            _swapMainWindowMethod.Invoke(view, new object[] { targetType });
        }

        private static Type ResolveSimulatorWindowType()
        {
            // Reuse DeviceSimulatorBridge's assembly resolution logic
            // by looking up the type directly from AppDomain
            System.Reflection.Assembly dsAsm = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "UnityEditor.DeviceSimulatorModule");
            if (dsAsm == null)
            {
                dsAsm = typeof(EditorWindow).Assembly;
            }

            return dsAsm.GetType("UnityEditor.DeviceSimulation.SimulatorWindow");
        }

        private static void EnsureMembersResolved()
        {
            if (_memberSearchDone)
            {
                return;
            }
            _memberSearchDone = true;

            System.Reflection.Assembly editorAssembly = typeof(Editor).Assembly;

            // --- PlayModeView ---
            _playModeViewType = editorAssembly.GetType("UnityEditor.PlayModeView");
            if (_playModeViewType == null)
            {
                Debug.LogWarning("[PlayModeViewBridge] PlayModeView type not found");
                return;
            }

            _getMainPlayModeViewMethod = _playModeViewType.GetMethod(
                "GetMainPlayModeView",
                BindingFlags.NonPublic | BindingFlags.Static);
            if (_getMainPlayModeViewMethod == null)
            {
                Debug.LogWarning("[PlayModeViewBridge] GetMainPlayModeView method not found");
            }

            _swapMainWindowMethod = _playModeViewType.GetMethod(
                "SwapMainWindow",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (_swapMainWindowMethod == null)
            {
                Debug.LogWarning("[PlayModeViewBridge] SwapMainWindow method not found");
            }

            // --- GameView ---
            _gameViewType = editorAssembly.GetType("UnityEditor.GameView");
            if (_gameViewType == null)
            {
                Debug.LogWarning("[PlayModeViewBridge] GameView type not found");
            }
        }
    }
}
