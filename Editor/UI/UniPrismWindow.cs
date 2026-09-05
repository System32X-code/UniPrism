using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UniPrism
{
    /// <summary>
    /// The whole of UniPrism's UI: pick a window, give it an image and two tints.
    /// </summary>
    internal sealed class UniPrismWindow : EditorWindow
    {
        private const string WindowTitle = "UniPrism";

        private string _selectedWindowTitle;
        private Vector2 _scrollPosition;
        private bool _showAbout;

        [MenuItem("Window/UniPrism")]
        private static void Open()
        {
            GetWindow<UniPrismWindow>(utility: false, title: WindowTitle);
        }

        private void OnGUI()
        {
            if (!HostViewBridge.IsAvailable)
            {
                EditorGUILayout.HelpBox(Loc.Tr("UniPrism is inactive: ") + HostViewBridge.UnavailableReason, MessageType.Error);
                return;
            }

            DrawToolbar();

            using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scroll.scrollPosition;

                if (_showAbout)
                {
                    AboutPage.Draw();
                    return;
                }

                DrawWindowSelector();

                if (string.IsNullOrEmpty(_selectedWindowTitle))
                {
                    EditorGUILayout.HelpBox(Loc.Tr("Pick a window to style."), MessageType.Info);
                    return;
                }

                DrawAppearance(ThemeStore.Theme.FindOrCreate(_selectedWindowTitle));
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(Loc.Tr("Theme"), GUILayout.ExpandWidth(false));

                var theme = ThemeStore.Theme;

                var changedName = EditorGUILayout.DelayedTextField(theme.Name, EditorStyles.toolbarTextField, GUILayout.Width(140f));
                if (changedName != theme.Name)
                {
                    theme.Name = changedName;
                    ThemeStore.MarkChanged();
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(Loc.Tr("Save"), EditorStyles.toolbarButton))
                {
                    ThemeStore.Save();
                }

                if (GUILayout.Button(Loc.Tr("Export"), EditorStyles.toolbarButton))
                {
                    Export();
                }

                if (GUILayout.Button(Loc.Tr("Import"), EditorStyles.toolbarButton))
                {
                    Import();
                }

                if (GUILayout.Button(Loc.Tr("Reset"), EditorStyles.toolbarButton))
                {
                    ResetTheme();
                }

                if (GUILayout.Button(Loc.ToggleLabel, EditorStyles.toolbarButton, GUILayout.ExpandWidth(false)))
                {
                    Loc.Toggle();
                }

                _showAbout = GUILayout.Toggle(_showAbout, Loc.Tr("About"), EditorStyles.toolbarButton, GUILayout.ExpandWidth(false));
            }
        }

        private void DrawWindowSelector()
        {
            var titles = OpenWindowTitles();

            if (titles.Count == 0)
            {
                EditorGUILayout.HelpBox(Loc.Tr("No editor windows found."), MessageType.Warning);
                return;
            }

            var selectedIndex = Mathf.Max(0, titles.IndexOf(_selectedWindowTitle));
            var newIndex = EditorGUILayout.Popup(Loc.Tr("Window"), selectedIndex, titles.ToArray());

            _selectedWindowTitle = titles[newIndex];
        }

        /// <summary>
        /// Titles of the windows currently open, plus any the theme already styles - a window can
        /// be configured, closed, and still needs to be reachable to edit or clear.
        /// </summary>
        private List<string> OpenWindowTitles()
        {
            var titles = new SortedSet<string>(System.StringComparer.Ordinal);

            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window == null || window is UniPrismWindow) continue;

                var title = window.titleContent?.text;
                if (!string.IsNullOrEmpty(title)) titles.Add(title);
            }

            foreach (var appearance in ThemeStore.Theme.Windows)
            {
                if (appearance != null && !string.IsNullOrEmpty(appearance.WindowTitle))
                {
                    titles.Add(appearance.WindowTitle);
                }
            }

            return titles.ToList();
        }

        private static void DrawAppearance(WindowAppearance appearance)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Loc.Tr("Background"), EditorStyles.boldLabel);

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                var theme = ThemeStore.Theme;
                var currentTexture = theme.FindTexture(appearance.BackgroundTextureId)?.Texture;

                var picked = EditorGUILayout.ObjectField(Loc.Tr("Image"), currentTexture, typeof(Texture2D), allowSceneObjects: false) as Texture2D;

                if (picked != currentTexture)
                {
                    // Encoded into the theme rather than referenced, so a theme stays portable and
                    // does not break when the source asset moves.
                    appearance.BackgroundTextureId = picked == null ? string.Empty : theme.AddTexture(picked)?.Id ?? string.Empty;
                }

                using (new EditorGUI.DisabledScope(!appearance.HasBackground))
                {
                    appearance.BackgroundTint = DrawTint(Loc.Tr("Image opacity"), Loc.Tr("Image tint"), appearance.BackgroundTint);
                    appearance.DrawOverContent = EditorGUILayout.Toggle(Loc.Tr("Draw over content"), appearance.DrawOverContent);
                }

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(Loc.Tr("Colours"), EditorStyles.boldLabel);

                appearance.BackdropTint = DrawTint(Loc.Tr("Backdrop opacity"), Loc.Tr("Backdrop tint"), appearance.BackdropTint);
                appearance.ContentTint = DrawTint(Loc.Tr("Text opacity"), Loc.Tr("Text and icon tint"), appearance.ContentTint);

                EditorGUILayout.HelpBox(Loc.Tr("Lower the backdrop tint's alpha to thin out the window's own backdrop so the image shows through. Text stays legible because it is tinted separately."), MessageType.Info);

                if (check.changed)
                {
                    ThemeStore.MarkChanged();
                }
            }

            EditorGUILayout.Space();

            if (GUILayout.Button(Loc.Tr("Reset this window")))
            {
                appearance.BackgroundTextureId = string.Empty;
                appearance.BackgroundTint = Color.white;
                appearance.DrawOverContent = false;
                appearance.BackdropTint = Color.white;
                appearance.ContentTint = Color.white;

                ThemeStore.MarkChanged();
            }
        }

        /// <summary>
        /// One tint is a colour and an opacity, and burying the opacity in a colour picker's alpha
        /// slider hides the control that matters most here. Stored as one Color all the same.
        /// </summary>
        private static Color DrawTint(string opacityLabel, string colourLabel, Color tint)
        {
            var opacity = EditorGUILayout.Slider(opacityLabel, tint.a, 0f, 1f);
            var colour = EditorGUILayout.ColorField(new GUIContent(colourLabel), tint, showEyedropper: true, showAlpha: false, hdr: false);

            return new Color(colour.r, colour.g, colour.b, opacity);
        }

        private static void Export()
        {
            var path = EditorUtility.SaveFilePanel(Loc.Tr("Export theme"), string.Empty, $"{ThemeStore.Theme.Name}.prism", "prism");
            if (string.IsNullOrEmpty(path)) return;

            ThemeStore.ExportToFile(path);
        }

        private static void Import()
        {
            var path = EditorUtility.OpenFilePanel(Loc.Tr("Import theme"), string.Empty, "prism");
            if (string.IsNullOrEmpty(path)) return;

            if (!ThemeStore.ImportFromFile(path))
            {
                EditorUtility.DisplayDialog(WindowTitle, Loc.Tr("Could not read that theme file."), Loc.Tr("Yes"));
            }
        }

        private static void ResetTheme()
        {
            if (!EditorUtility.DisplayDialog(WindowTitle, Loc.Tr("Reset every window in this theme?"), Loc.Tr("Yes"), Loc.Tr("No"))) return;

            ThemeStore.Replace(new Theme());
            ThemeStore.Save();
        }
    }
}
