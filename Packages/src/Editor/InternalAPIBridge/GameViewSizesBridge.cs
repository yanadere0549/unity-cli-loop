using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace io.github.hatayama.uLoopMCP
{
    /// <summary>
    /// Bridge class for accessing Unity GameViewSizes internal APIs via reflection.
    /// Provides access to game view size management (display texts, adding custom sizes, index selection).
    /// </summary>
    public static class GameViewSizesBridge
    {
        // GameView types and members
        private static Type _gameViewType;
        private static PropertyInfo _selectedSizeIndexProp;

        // GameViewSizes types and members
        private static Type _gameViewSizesType;
        private static PropertyInfo _instanceProp;
        private static PropertyInfo _currentGroupProp;
        private static MethodInfo _changedMethod;

        // GameViewSizeGroup methods
        private static MethodInfo _getDisplayTextsMethod;
        private static MethodInfo _addCustomSizeMethod;
        private static MethodInfo _getTotalCountMethod;

        // GameViewSize constructor and type
        private static Type _gameViewSizeType;
        private static Type _gameViewSizeTypeEnum;
        private static ConstructorInfo _gameViewSizeCtor;
        private static object _fixedResolutionEnumValue;

        private static bool _memberSearchDone;

        /// <summary>
        /// Returns display texts for all game view sizes in the current group.
        /// Returns an empty array if the types are not available.
        /// </summary>
        public static string[] GetDisplayTexts()
        {
            EnsureMembersResolved();

            object group = GetCurrentGroup();
            if (group == null || _getDisplayTextsMethod == null)
            {
                return Array.Empty<string>();
            }

            string[] result = _getDisplayTextsMethod.Invoke(group, null) as string[];
            return result ?? Array.Empty<string>();
        }

        /// <summary>
        /// Returns the currently selected size index for the given GameView window.
        /// Returns -1 if the window is null or the property is not available.
        /// </summary>
        public static int GetSelectedSizeIndex(EditorWindow gameView)
        {
            EnsureMembersResolved();

            if (gameView == null || _selectedSizeIndexProp == null)
            {
                return -1;
            }

            object result = _selectedSizeIndexProp.GetValue(gameView);
            return result is int i ? i : -1;
        }

        /// <summary>
        /// Sets the selected size index on the given GameView window.
        /// No-op if the window is null or the property is not available.
        /// </summary>
        public static void SetSelectedSizeIndex(EditorWindow gameView, int index)
        {
            EnsureMembersResolved();

            if (gameView == null || _selectedSizeIndexProp == null)
            {
                return;
            }

            _selectedSizeIndexProp.SetValue(gameView, index);
        }

        /// <summary>
        /// Returns the total number of sizes (built-in + custom) in the current group.
        /// Returns -1 if the types are not available.
        /// </summary>
        public static int GetTotalCount()
        {
            EnsureMembersResolved();

            object group = GetCurrentGroup();
            if (group == null || _getTotalCountMethod == null)
            {
                return -1;
            }

            object result = _getTotalCountMethod.Invoke(group, null);
            return result is int i ? i : -1;
        }

        /// <summary>
        /// Adds a custom FixedResolution game view size with the given dimensions and label.
        /// Returns the index of the newly added size, or -1 on failure.
        /// </summary>
        public static int AddCustomSize(int w, int h, string label)
        {
            EnsureMembersResolved();

            object group = GetCurrentGroup();
            if (group == null
                || _addCustomSizeMethod == null
                || _gameViewSizeCtor == null
                || _fixedResolutionEnumValue == null
                || _changedMethod == null
                || _instanceProp == null)
            {
                return -1;
            }

            object newSize = _gameViewSizeCtor.Invoke(new object[] { _fixedResolutionEnumValue, w, h, label });
            if (newSize == null)
            {
                return -1;
            }

            _addCustomSizeMethod.Invoke(group, new object[] { newSize });

            object instance = _instanceProp.GetValue(null);
            if (instance != null)
            {
                _changedMethod.Invoke(instance, null);
            }

            // The new entry is appended at the end; return the last index
            int total = GetTotalCount();
            return total > 0 ? total - 1 : -1;
        }

        /// <summary>
        /// Finds the first size whose display text contains the given substring (case-insensitive).
        /// Returns the index, or -1 if not found.
        /// </summary>
        public static int FindSizeByLabel(string labelSubstring)
        {
            string[] texts = GetDisplayTexts();
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].IndexOf(labelSubstring, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Finds an open GameView EditorWindow, or null if none is open.
        /// </summary>
        public static EditorWindow FindGameViewWindow()
        {
            EnsureMembersResolved();

            if (_gameViewType == null)
            {
                return null;
            }

            UnityEngine.Object[] views = Resources.FindObjectsOfTypeAll(_gameViewType);
            if (views.Length == 0)
            {
                return null;
            }

            return views[0] as EditorWindow;
        }

        // --- private helpers ---

        private static object GetCurrentGroup()
        {
            if (_instanceProp == null || _currentGroupProp == null)
            {
                return null;
            }

            object instance = _instanceProp.GetValue(null);
            if (instance == null)
            {
                return null;
            }

            return _currentGroupProp.GetValue(instance);
        }

        private static void EnsureMembersResolved()
        {
            if (_memberSearchDone)
            {
                return;
            }
            _memberSearchDone = true;

            System.Reflection.Assembly editorAssembly = typeof(Editor).Assembly;

            // --- GameView ---
            _gameViewType = editorAssembly.GetType("UnityEditor.GameView");
            if (_gameViewType == null)
            {
                Debug.LogWarning("[GameViewSizesBridge] GameView type not found");
            }
            else
            {
                _selectedSizeIndexProp = _gameViewType.GetProperty(
                    "selectedSizeIndex",
                    BindingFlags.Public | BindingFlags.Instance);
                if (_selectedSizeIndexProp == null)
                {
                    Debug.LogWarning("[GameViewSizesBridge] selectedSizeIndex property not found");
                }
            }

            // --- GameViewSizes ---
            _gameViewSizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
            if (_gameViewSizesType == null)
            {
                Debug.LogWarning("[GameViewSizesBridge] GameViewSizes type not found");
                return;
            }

            // instance is inherited from ScriptableSingleton<T> — FlattenHierarchy is required
            _instanceProp = _gameViewSizesType.GetProperty(
                "instance",
                BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
            if (_instanceProp == null)
            {
                Debug.LogWarning("[GameViewSizesBridge] GameViewSizes.instance property not found");
            }

            // currentGroup is internal on 6000.0 — probe NonPublic first, fallback to Public
            _currentGroupProp = _gameViewSizesType.GetProperty(
                "currentGroup",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (_currentGroupProp == null)
            {
                _currentGroupProp = _gameViewSizesType.GetProperty(
                    "currentGroup",
                    BindingFlags.Public | BindingFlags.Instance);
            }
            if (_currentGroupProp == null)
            {
                Debug.LogWarning("[GameViewSizesBridge] currentGroup property not found");
            }

            _changedMethod = _gameViewSizesType.GetMethod(
                "Changed",
                BindingFlags.Public | BindingFlags.Instance);
            if (_changedMethod == null)
            {
                Debug.LogWarning("[GameViewSizesBridge] Changed() method not found");
            }

            // --- GameViewSizeGroup ---
            Type groupType = editorAssembly.GetType("UnityEditor.GameViewSizeGroup");
            if (groupType == null)
            {
                Debug.LogWarning("[GameViewSizesBridge] GameViewSizeGroup type not found");
                return;
            }

            _getDisplayTextsMethod = groupType.GetMethod(
                "GetDisplayTexts",
                BindingFlags.Public | BindingFlags.Instance);
            if (_getDisplayTextsMethod == null)
            {
                Debug.LogWarning("[GameViewSizesBridge] GetDisplayTexts method not found");
            }

            _addCustomSizeMethod = groupType.GetMethod(
                "AddCustomSize",
                BindingFlags.Public | BindingFlags.Instance);
            if (_addCustomSizeMethod == null)
            {
                Debug.LogWarning("[GameViewSizesBridge] AddCustomSize method not found");
            }

            _getTotalCountMethod = groupType.GetMethod(
                "GetTotalCount",
                BindingFlags.Public | BindingFlags.Instance);
            if (_getTotalCountMethod == null)
            {
                Debug.LogWarning("[GameViewSizesBridge] GetTotalCount method not found");
            }

            // --- GameViewSize constructor ---
            _gameViewSizeType = editorAssembly.GetType("UnityEditor.GameViewSize");
            _gameViewSizeTypeEnum = editorAssembly.GetType("UnityEditor.GameViewSizeType");
            if (_gameViewSizeType == null || _gameViewSizeTypeEnum == null)
            {
                Debug.LogWarning("[GameViewSizesBridge] GameViewSize or GameViewSizeType type not found");
                return;
            }

            _gameViewSizeCtor = _gameViewSizeType.GetConstructor(
                new Type[] { _gameViewSizeTypeEnum, typeof(int), typeof(int), typeof(string) });
            if (_gameViewSizeCtor == null)
            {
                Debug.LogWarning("[GameViewSizesBridge] GameViewSize(type,w,h,label) constructor not found");
            }

            try
            {
                _fixedResolutionEnumValue = Enum.Parse(_gameViewSizeTypeEnum, "FixedResolution");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GameViewSizesBridge] FixedResolution enum value not found: {ex.Message}");
            }
        }
    }
}
