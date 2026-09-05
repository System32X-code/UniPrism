using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UniPrism
{
    /// <summary>
    /// The whole of UniPrism's dependency on Unity's internals, in one place.
    /// </summary>
    /// <remarks>
    /// Every editor window is shown by a <c>UnityEditor.HostView</c>, which is internal, and the
    /// delegate it invokes for the window's own OnGUI is protected. Reflection is not a shortcut
    /// here - the field could not be reached any other way - so rather than spread it around, it
    /// all lives here and every member fails soft: if a future Unity renames something, UniPrism goes
    /// inert and says so instead of throwing on every repaint.
    /// </remarks>
    internal static class HostViewBridge
    {
        private const string HostViewTypeName = "UnityEditor.HostView";
        private const string OnGUIFieldName = "m_OnGUI";
        private const string ActualViewPropertyName = "actualView";

        private static bool _resolved;
        private static Type _hostViewType;
        private static FieldInfo _onGUIField;
        private static PropertyInfo _actualViewProperty;

        /// <summary>
        /// Why the bridge is unavailable, for the diagnostics report. Null when everything resolved.
        /// </summary>
        public static string UnavailableReason { get; private set; }

        public static bool IsAvailable
        {
            get
            {
                Resolve();

                return UnavailableReason == null;
            }
        }

        /// <summary>
        /// The delegate type of the OnGUI field, needed to build a replacement of the same type.
        /// </summary>
        public static Type OnGUIDelegateType
        {
            get
            {
                Resolve();

                return _onGUIField?.FieldType;
            }
        }

        public static IEnumerable<ScriptableObject> GetHostViews()
        {
            Resolve();

            if (_hostViewType == null) yield break;

            // The non-generic overload takes a Type, which is what lets an internal type be
            // enumerated without naming it at compile time.
            foreach (var candidate in Resources.FindObjectsOfTypeAll(_hostViewType))
            {
                if (candidate is ScriptableObject hostView)
                {
                    yield return hostView;
                }
            }
        }

        public static EditorWindow GetActualView(ScriptableObject hostView)
        {
            Resolve();

            if (hostView == null || _actualViewProperty == null) return null;

            try
            {
                return _actualViewProperty.GetValue(hostView) as EditorWindow;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Delegate GetOnGUI(ScriptableObject hostView)
        {
            Resolve();

            if (hostView == null || _onGUIField == null) return null;

            try
            {
                return _onGUIField.GetValue(hostView) as Delegate;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static bool SetOnGUI(ScriptableObject hostView, Delegate value)
        {
            Resolve();

            if (hostView == null || _onGUIField == null) return false;

            try
            {
                _onGUIField.SetValue(hostView, value);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void Resolve()
        {
            if (_resolved) return;

            _resolved = true;

            _hostViewType = typeof(EditorWindow).Assembly.GetType(HostViewTypeName, throwOnError: false);
            if (_hostViewType == null)
            {
                UnavailableReason = $"{HostViewTypeName} not found";
                return;
            }

            _onGUIField = _hostViewType.GetField(OnGUIFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (_onGUIField == null || !typeof(Delegate).IsAssignableFrom(_onGUIField.FieldType))
            {
                UnavailableReason = $"{HostViewTypeName}.{OnGUIFieldName} not found, or is not a delegate";
                return;
            }

            _actualViewProperty = _hostViewType.GetProperty(ActualViewPropertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (_actualViewProperty == null)
            {
                UnavailableReason = $"{HostViewTypeName}.{ActualViewPropertyName} not found";
            }
        }
    }
}
