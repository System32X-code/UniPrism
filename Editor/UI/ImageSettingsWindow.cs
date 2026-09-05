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

        public static void Open(string windowTitle)
        {
            var window = GetWindow<ImageSettingsWindow>(utility: true, title: Loc.Tr("Image settings"));
            window._windowTitle = windowTitle;
            window.minSize = new Vector2(360f, 340f);
            window.Show();
        }

        private void OnGUI()
        {
            if (string.IsNullOrEmpty(_windowTitle))
            {
                EditorGUILayout.HelpBox(Loc.Tr("Pick a window to style."), MessageType.Info);
                return;
            }

            var appearance = ThemeStore.Theme.Find(_windowTitle);
            if (appearance == null || !appearance.HasBackground)
            {
                EditorGUILayout.HelpBox(Loc.Tr("This window has no background image."), MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField(_windowTitle, EditorStyles.boldLabel);

            DrawPreview(appearance);

            EditorGUILayout.Space();

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                appearance.ImageScaleMode = (ImageScaleMode)EditorGUILayout.Popup(
                    Loc.Tr("Scale mode"),
                    (int)appearance.ImageScaleMode,
                    new[] { Loc.Tr("Crop"), Loc.Tr("Fit"), Loc.Tr("Stretch") });

                using (new EditorGUI.DisabledScope(appearance.ImageScaleMode != ImageScaleMode.Crop))
                {
                    appearance.ImageZoom = EditorGUILayout.Slider(Loc.Tr("Zoom"), appearance.ImageZoom, 1f, 4f);

                    var alignment = appearance.ImageAlignment;
                    alignment.x = EditorGUILayout.Slider(Loc.Tr("Horizontal"), alignment.x, 0f, 1f);
                    alignment.y = EditorGUILayout.Slider(Loc.Tr("Vertical"), alignment.y, 0f, 1f);
                    appearance.ImageAlignment = alignment;

                    if (GUILayout.Button(Loc.Tr("Centre")))
                    {
                        appearance.ImageAlignment = new Vector2(0.5f, 0.5f);
                        appearance.ImageZoom = 1f;
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

        private void DrawPreview(WindowAppearance appearance)
        {
            var texture = ThemeStore.Theme.FindTexture(appearance.BackgroundTextureId)?.Texture;
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
            GUI.color = previousColor * appearance.BackgroundTint;

            try
            {
                WindowPainter.DrawFramedPreview(rect, texture, appearance);
            }
            finally
            {
                GUI.color = previousColor;
            }
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
