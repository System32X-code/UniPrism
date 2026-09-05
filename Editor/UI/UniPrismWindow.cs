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

                DrawPalette();

                EditorGUILayout.Space();

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

                Loc.Current = (PrismLanguage)EditorGUILayout.Popup(
                    (int)Loc.Current, Loc.LanguageNames, EditorStyles.toolbarPopup, GUILayout.Width(84f));

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

                Texture2D picked;

                using (new EditorGUILayout.HorizontalScope())
                {
                    picked = EditorGUILayout.ObjectField(Loc.Tr("Image"), currentTexture, typeof(Texture2D), allowSceneObjects: false) as Texture2D;

                    using (new EditorGUI.DisabledScope(!appearance.HasBackground))
                    {
                        if (GUILayout.Button(Loc.Tr("Framing..."), GUILayout.Width(90f)))
                        {
                            ImageSettingsWindow.Open(appearance.WindowTitle);
                        }
                    }
                }

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

                appearance.BackdropSlot = (PaletteSlot)EditorGUILayout.Popup(
                    Loc.Tr("Colour source"),
                    (int)appearance.BackdropSlot,
                    new[] { Loc.Tr("Custom"), Loc.Tr("Primary"), Loc.Tr("Secondary"), Loc.Tr("Accent") });

                appearance.BackdropTint = DrawTint(
                    Loc.Tr("Backdrop opacity"),
                    Loc.Tr("Backdrop tint"),
                    appearance.BackdropTint,
                    // Driven by the palette: still shown, so it is clear which colour is in play,
                    // but edited from the palette section rather than here.
                    overrideColour: appearance.BackdropSlot == PaletteSlot.Custom
                        ? (Color?)null
                        : ThemeStore.Theme.Palette.Resolve(appearance.BackdropSlot));
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
        private static Color DrawTint(string opacityLabel, string colourLabel, Color tint, Color? overrideColour = null)
        {
            var opacity = EditorGUILayout.Slider(opacityLabel, tint.a, 0f, 1f);

            using (new EditorGUI.DisabledScope(overrideColour.HasValue))
            {
                var shown = overrideColour ?? tint;
                var edited = EditorGUILayout.ColorField(new GUIContent(colourLabel), shown, showEyedropper: true, showAlpha: false, hdr: false);

                //Opacity stays this window's own even when the colour comes from the palette.
                var colour = overrideColour.HasValue ? tint : edited;

                return new Color(colour.r, colour.g, colour.b, opacity);
            }
        }

        private void DrawPalette()
        {
            var palette = ThemeStore.Theme.Palette;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Loc.Tr("Palette"), EditorStyles.boldLabel);

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                palette.Primary = EditorGUILayout.ColorField(new GUIContent(Loc.Tr("Primary")), palette.Primary, true, false, false);
                palette.Secondary = EditorGUILayout.ColorField(new GUIContent(Loc.Tr("Secondary")), palette.Secondary, true, false, false);
                palette.Accent = EditorGUILayout.ColorField(new GUIContent(Loc.Tr("Accent")), palette.Accent, true, false, false);

                if (check.changed)
                {
                    ThemeStore.MarkChanged();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(Loc.Tr("Apply Primary to all windows")))
                {
                    ApplyToAllWindows(PaletteSlot.Primary);
                }

                if (GUILayout.Button(Loc.Tr("Unlink all")))
                {
                    ApplyToAllWindows(PaletteSlot.Custom);
                }
            }

            EditorGUILayout.HelpBox(Loc.Tr("Point windows at a palette slot and editing that colour recolours all of them at once. Put one group on Primary and another on Accent for contrast."), MessageType.Info);
        }

        /// <summary>
        /// Covers every window currently open as well as every one the theme already knows, so a
        /// fresh theme does not have to be filled in window by window first.
        /// </summary>
        private void ApplyToAllWindows(PaletteSlot slot)
        {
            var theme = ThemeStore.Theme;
            var applied = 0;

            foreach (var title in OpenWindowTitles())
            {
                var appearance = theme.FindOrCreate(title);
                appearance.BackdropSlot = slot;

                //Fully opaque would show none of the colour at all, which just reads as broken.
                if (slot != PaletteSlot.Custom && Mathf.Approximately(appearance.BackdropTint.a, 1f))
                {
                    var tint = appearance.BackdropTint;
                    appearance.BackdropTint = new Color(tint.r, tint.g, tint.b, 0.6f);
                }

                applied++;
            }

            ThemeStore.MarkChanged();

            Debug.Log($"UniPrism: applied {slot} to {applied} window(s).");
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
