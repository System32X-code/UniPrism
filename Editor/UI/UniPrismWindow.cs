using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UniPrism
{
    /// <summary>
    /// UniPrism's window: a global look, per-window departures from it, and an about page.
    /// </summary>
    internal sealed class UniPrismWindow : EditorWindow
    {
        private const string Title = "UniPrism";

        private enum Page
        {
            Global = 0,
            Window = 1,
            About = 2
        }

        private Page _page;
        private string _selectedWindowTitle;
        private Vector2 _scrollPosition;

        [MenuItem("Window/UniPrism")]
        private static void Open()
        {
            GetWindow<UniPrismWindow>(utility: false, title: Title);
        }

        private void OnGUI()
        {
            if (!HostViewBridge.IsAvailable)
            {
                EditorGUILayout.HelpBox(Loc.Tr("UniPrism is inactive: ") + HostViewBridge.UnavailableReason, MessageType.Error);
                return;
            }

            DrawToolbar();
            DrawPageSelector();

            using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scroll.scrollPosition;

                switch (_page)
                {
                    case Page.Global:
                        DrawGlobalPage();
                        break;
                    case Page.Window:
                        DrawWindowPage();
                        break;
                    default:
                        AboutPage.Draw();
                        break;
                }
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label(Loc.Tr("Theme"), GUILayout.ExpandWidth(false));

                var theme = ThemeStore.Theme;

                var changedName = EditorGUILayout.DelayedTextField(theme.Name, EditorStyles.toolbarTextField, GUILayout.Width(130f));
                if (changedName != theme.Name)
                {
                    theme.Name = changedName;
                    ThemeStore.MarkChanged();
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(Loc.Tr("Save"), EditorStyles.toolbarButton)) ThemeStore.Save();
                if (GUILayout.Button(Loc.Tr("Export"), EditorStyles.toolbarButton)) Export();
                if (GUILayout.Button(Loc.Tr("Import"), EditorStyles.toolbarButton)) Import();
                if (GUILayout.Button(Loc.Tr("Reset"), EditorStyles.toolbarButton)) ResetTheme();

                Loc.Current = (PrismLanguage)EditorGUILayout.Popup(
                    (int)Loc.Current, Loc.LanguageNames, EditorStyles.toolbarPopup, GUILayout.Width(84f));
            }
        }

        private void DrawPageSelector()
        {
            var labels = new[] { Loc.Tr("Global"), Loc.Tr("Per window"), Loc.Tr("About") };

            _page = (Page)GUILayout.Toolbar((int)_page, labels, GUILayout.Height(22f));

            EditorGUILayout.Space();
        }

        // ------------------------------------------------------------------ global

        private void DrawGlobalPage()
        {
            var theme = ThemeStore.Theme;

            DrawPalette(theme.Palette);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(Loc.Tr("Background"), EditorStyles.boldLabel);

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                DrawBackground(theme.Global, global: true);

                EditorGUILayout.Space();
                EditorGUILayout.LabelField(Loc.Tr("Colours"), EditorStyles.boldLabel);

                DrawColours(theme.Global);

                if (check.changed) ThemeStore.MarkChanged();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(Loc.Tr("Everything here applies to every window. Give a single window something different on the Per window page."), MessageType.Info);
        }

        private void DrawPalette(Palette palette)
        {
            EditorGUILayout.LabelField(Loc.Tr("Palette"), EditorStyles.boldLabel);

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                palette.Primary = EditorGUILayout.ColorField(new GUIContent(Loc.Tr("Primary")), palette.Primary, true, false, false);
                palette.Secondary = EditorGUILayout.ColorField(new GUIContent(Loc.Tr("Secondary")), palette.Secondary, true, false, false);
                palette.Tertiary = EditorGUILayout.ColorField(new GUIContent(Loc.Tr("Tertiary")), palette.Tertiary, true, false, false);

                if (check.changed) ThemeStore.MarkChanged();
            }

            EditorGUILayout.HelpBox(Loc.Tr("Anything set to a palette colour follows it, so editing one colour here recolours everything using it at once."), MessageType.Info);
        }

        // ------------------------------------------------------------------ per window

        private void DrawWindowPage()
        {
            var titles = KnownWindowTitles();

            if (titles.Count == 0)
            {
                EditorGUILayout.HelpBox(Loc.Tr("No editor windows found."), MessageType.Warning);
                return;
            }

            var selectedIndex = Mathf.Max(0, titles.IndexOf(_selectedWindowTitle));
            _selectedWindowTitle = titles[EditorGUILayout.Popup(Loc.Tr("Window"), selectedIndex, titles.ToArray())];

            var appearance = ThemeStore.Theme.FindOrCreate(_selectedWindowTitle);

            EditorGUILayout.Space();

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                appearance.OverridesBackground = EditorGUILayout.ToggleLeft(
                    Loc.Tr("Override the global background"), appearance.OverridesBackground, EditorStyles.boldLabel);

                using (new EditorGUI.DisabledScope(!appearance.OverridesBackground))
                {
                    EditorGUI.indentLevel++;
                    DrawBackground(appearance.Settings, global: false);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.Space();

                appearance.OverridesColours = EditorGUILayout.ToggleLeft(
                    Loc.Tr("Override the global colours"), appearance.OverridesColours, EditorStyles.boldLabel);

                using (new EditorGUI.DisabledScope(!appearance.OverridesColours))
                {
                    EditorGUI.indentLevel++;
                    DrawColours(appearance.Settings);
                    EditorGUI.indentLevel--;
                }

                if (check.changed) ThemeStore.MarkChanged();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(Loc.Tr("With both switches off this window simply follows the global look."), MessageType.Info);

            if (GUILayout.Button(Loc.Tr("Follow global again")))
            {
                appearance.OverridesBackground = false;
                appearance.OverridesColours = false;

                ThemeStore.MarkChanged();
            }
        }

        // ------------------------------------------------------------------ shared blocks

        private static void DrawBackground(AppearanceSettings settings, bool global)
        {
            var theme = ThemeStore.Theme;
            var currentTexture = theme.FindTexture(settings.BackgroundTextureId)?.Texture;

            Texture2D picked;

            using (new EditorGUILayout.HorizontalScope())
            {
                picked = EditorGUILayout.ObjectField(Loc.Tr("Image"), currentTexture, typeof(Texture2D), allowSceneObjects: false) as Texture2D;

                // The object picker only lists textures already imported into the project, which
                // means importing a wallpaper into a project it has nothing to do with. A theme
                // stores bytes, so the file can be read straight off disk instead.
                if (GUILayout.Button(Loc.Tr("Browse..."), GUILayout.Width(80f)))
                {
                    BrowseForImage(settings);
                }

                using (new EditorGUI.DisabledScope(!settings.HasBackground))
                {
                    if (GUILayout.Button(Loc.Tr("Framing..."), GUILayout.Width(90f)))
                    {
                        //A null title means the global image.
                        ImageSettingsWindow.Open(global ? null : CurrentWindowTitleOf(settings));
                    }
                }
            }

            if (picked != currentTexture)
            {
                // Encoded into the theme rather than referenced, so a theme stays portable and does
                // not break when the source asset moves.
                settings.BackgroundTextureId = picked == null ? string.Empty : theme.AddTexture(picked)?.Id ?? string.Empty;
            }

            using (new EditorGUI.DisabledScope(!settings.HasBackground))
            {
                settings.BackgroundTint = DrawTint(Loc.Tr("Image opacity"), Loc.Tr("Image tint"), settings.BackgroundTint);
                settings.DrawOverContent = EditorGUILayout.Toggle(Loc.Tr("Draw over content"), settings.DrawOverContent);

                if (global)
                {
                    settings.SpanEditor = EditorGUILayout.Toggle(Loc.Tr("One image across the editor"), settings.SpanEditor);

                    if (settings.SpanEditor)
                    {
                        EditorGUILayout.HelpBox(Loc.Tr("The image is laid over the whole editor and each window shows the part behind it, instead of every window scaling its own copy."), MessageType.Info);
                    }
                }
            }
        }

        private static void DrawColours(AppearanceSettings settings)
        {
            DrawTintRow(Loc.Tr("Window backdrop"), Loc.Tr("Backdrop opacity"), Loc.Tr("Backdrop tint"),
                () => settings.BackdropSlot, slot => settings.BackdropSlot = slot,
                () => settings.BackdropTint, tint => settings.BackdropTint = tint);

            DrawTintRow(Loc.Tr("Text"), Loc.Tr("Text opacity"), Loc.Tr("Text tint"),
                () => settings.ContentSlot, slot => settings.ContentSlot = slot,
                () => settings.ContentTint, tint => settings.ContentTint = tint);

            settings.TintIcons = EditorGUILayout.Toggle(Loc.Tr("Tint icons too"), settings.TintIcons);

            DrawTintRow(Loc.Tr("Window frame"), Loc.Tr("Frame strength"), Loc.Tr("Frame tint"),
                () => settings.ChromeSlot, slot => settings.ChromeSlot = slot,
                () => settings.ChromeTint, tint => settings.ChromeTint = tint);

            EditorGUILayout.HelpBox(Loc.Tr("The backdrop thins out to reveal a background image. Text is tinted through the styles, so icons keep their own colours unless you ask for them. The frame is the dock's tab strip, washed over rather than tinted."), MessageType.Info);
        }

        /// <summary>
        /// One colour: where it comes from, how strong it is, and the colour itself.
        /// </summary>
        private static void DrawTintRow(
            string header, string opacityLabel, string colourLabel,
            Func<PaletteSlot> getSlot, Action<PaletteSlot> setSlot,
            Func<Color> getTint, Action<Color> setTint)
        {
            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField(header, EditorStyles.miniBoldLabel);

            var slot = (PaletteSlot)EditorGUILayout.Popup(
                Loc.Tr("Colour source"),
                (int)getSlot(),
                new[] { Loc.Tr("Custom"), Loc.Tr("Primary"), Loc.Tr("Secondary"), Loc.Tr("Tertiary") });

            setSlot(slot);

            var fromPalette = slot != PaletteSlot.Custom;

            setTint(DrawTint(
                opacityLabel,
                colourLabel,
                getTint(),
                //Shown greyed out rather than hidden, so it is clear which colour is in play.
                fromPalette ? ThemeStore.Theme.Palette.Resolve(slot) : (Color?)null));
        }

        /// <summary>
        /// A tint is a colour and an opacity. The opacity gets its own slider rather than hiding
        /// in the colour picker's alpha, because it is the control reached for most often.
        /// </summary>
        private static Color DrawTint(string opacityLabel, string colourLabel, Color tint, Color? fromPalette = null)
        {
            var opacity = EditorGUILayout.Slider(opacityLabel, tint.a, 0f, 1f);

            using (new EditorGUI.DisabledScope(fromPalette.HasValue))
            {
                //Shown greyed rather than hidden, so it is clear which colour is in play.
                var shown = fromPalette ?? tint;
                var edited = EditorGUILayout.ColorField(new GUIContent(colourLabel), shown, true, false, false);
                var colour = fromPalette.HasValue ? tint : edited;

                return new Color(colour.r, colour.g, colour.b, opacity);
            }
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>
        /// Windows currently open, plus any the theme already mentions - a window can be
        /// configured, closed, and still needs to be reachable to edit or clear.
        /// </summary>
        private List<string> KnownWindowTitles()
        {
            var titles = new SortedSet<string>(StringComparer.Ordinal);

            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window == null || window is UniPrismWindow || window is ImageSettingsWindow) continue;

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

        private static string CurrentWindowTitleOf(AppearanceSettings settings)
        {
            foreach (var appearance in ThemeStore.Theme.Windows)
            {
                if (appearance != null && ReferenceEquals(appearance.Settings, settings)) return appearance.WindowTitle;
            }

            return null;
        }

        private static void BrowseForImage(AppearanceSettings settings)
        {
            var path = EditorUtility.OpenFilePanelWithFilters(
                Loc.Tr("Choose an image"), string.Empty, new[] { Loc.Tr("Images"), "png,jpg,jpeg" });

            if (string.IsNullOrEmpty(path)) return;

            var texture = ThemeStore.Theme.AddTextureFromFile(path);
            if (texture == null)
            {
                EditorUtility.DisplayDialog(Title, Loc.Tr("That file could not be read as an image."), Loc.Tr("Yes"));
                return;
            }

            settings.BackgroundTextureId = texture.Id;

            ThemeStore.MarkChanged();
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
                EditorUtility.DisplayDialog(Title, Loc.Tr("Could not read that theme file."), Loc.Tr("Yes"));
            }
        }

        private static void ResetTheme()
        {
            if (!EditorUtility.DisplayDialog(Title, Loc.Tr("Reset every window in this theme?"), Loc.Tr("Yes"), Loc.Tr("No"))) return;

            ThemeStore.Replace(new Theme());
            ThemeStore.Save();
        }
    }
}
