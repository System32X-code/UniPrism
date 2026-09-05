using UnityEditor;
using UnityEngine;

namespace UniPrism
{
    /// <summary>
    /// Framing controls for one window's background image, with a preview.
    /// </summary>
    /// <remarks>
    /// Cropping by numbers is guesswork, and the window being styled is usually behind whatever
    /// you are working in - so the preview draws the image exactly the way the painter will,
    /// through the same code, at the aspect ratio of the target window.
    /// </remarks>
    internal sealed class ImageSettingsWindow : EditorWindow
    {
        private const float PreviewHeight = 150f;

        [SerializeField]
        private string _windowTitle;
        [SerializeField]
        private bool _global;

        /// <summary>Pass a null title to frame the theme's global image.</summary>
        public static void Open(string windowTitle)
        {
            var window = GetWindow<ImageSettingsWindow>(utility: true, title: Loc.Tr("Image settings"));
            window._global = string.IsNullOrEmpty(windowTitle);
            window._windowTitle = windowTitle;
            window.minSize = new Vector2(360f, 340f);
            window.Show();
        }

        private void OnGUI()
        {
            var settings = ResolveSettings();
            if (settings == null || !settings.HasBackground)
            {
                EditorGUILayout.HelpBox(Loc.Tr("This window has no background image."), MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField(_global ? Loc.Tr("Global") : _windowTitle, EditorStyles.boldLabel);

            DrawPreview(settings);

            EditorGUILayout.Space();

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                settings.ImageScaleMode = (ImageScaleMode)EditorGUILayout.Popup(
                    Loc.Tr("Scale mode"),
                    (int)settings.ImageScaleMode,
                    new[] { Loc.Tr("Crop"), Loc.Tr("Fit"), Loc.Tr("Stretch") });

                using (new EditorGUI.DisabledScope(settings.ImageScaleMode != ImageScaleMode.Crop))
                {
                    settings.ImageZoom = EditorGUILayout.Slider(Loc.Tr("Zoom"), settings.ImageZoom, 1f, 4f);

                    var alignment = settings.ImageAlignment;
                    alignment.x = EditorGUILayout.Slider(Loc.Tr("Horizontal"), alignment.x, 0f, 1f);
                    alignment.y = EditorGUILayout.Slider(Loc.Tr("Vertical"), alignment.y, 0f, 1f);
                    settings.ImageAlignment = alignment;

                    if (GUILayout.Button(Loc.Tr("Centre")))
                    {
                        settings.ImageAlignment = new Vector2(0.5f, 0.5f);
                        settings.ImageZoom = 1f;
                        GUI.changed = true;
                    }
                }

                if (check.changed)
                {
                    ThemeStore.MarkChanged();
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(Loc.Tr("Fit and Stretch use the whole image, so zoom and alignment only apply to Crop."), MessageType.Info);
        }

        private void DrawPreview(AppearanceSettings settings)
        {
            var texture = ThemeStore.Theme.FindTexture(settings.BackgroundTextureId)?.Texture;
            if (texture == null)
            {
                EditorGUILayout.HelpBox(Loc.Tr("The image could not be decoded."), MessageType.Warning);
                return;
            }

            var rect = GUILayoutUtility.GetRect(0f, PreviewHeight, GUILayout.ExpandWidth(true));

            //Matched to the window being styled, so the preview crops the way the real one will.
            var target = FindTargetWindow();
            if (target != null && target.position.height > 0f)
            {
                var aspect = target.position.width / target.position.height;
                var width = Mathf.Min(rect.width, rect.height * aspect);

                rect = new Rect(rect.x + (rect.width - width) * 0.5f, rect.y, width, rect.height);
            }

            EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, 0.35f));

            var previousColor = GUI.color;
            GUI.color = previousColor * settings.BackgroundTint;

            try
            {
                ImageFraming.Draw(rect, texture, settings, spanEditor: false);
            }
            finally
            {
                GUI.color = previousColor;
            }
        }

        private AppearanceSettings ResolveSettings()
        {
            if (_global) return ThemeStore.Theme.Global;

            if (string.IsNullOrEmpty(_windowTitle)) return null;

            var window = ThemeStore.Theme.Find(_windowTitle);

            return window != null && window.OverridesBackground ? window.Settings : null;
        }

        private EditorWindow FindTargetWindow()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window == null || window is ImageSettingsWindow) continue;

                if (window.titleContent?.text == _windowTitle) return window;
            }

            return null;
        }
    }
}
