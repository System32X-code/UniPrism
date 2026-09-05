using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UniPrism
{
    /// <summary>
    /// Holds the active theme.
    /// </summary>
    /// <remarks>
    /// Stored in the editor preferences folder rather than in the project: a theme is how *you*
    /// want the editor to look, not a property of whatever you happen to have open, so it follows
    /// the machine across projects and Unity versions.
    /// </remarks>
    [FilePath("Theme.asset", FilePathAttribute.Location.PreferencesFolder)]
    internal sealed class ThemeStore : ScriptableSingleton<ThemeStore>
    {
        [SerializeField]
        private Theme _theme;

        public static event Action Changed = () => { };

        public static Theme Theme
        {
            get
            {
                if (instance._theme == null)
                {
                    instance._theme = new Theme();
                }

                return instance._theme;
            }
        }

        /// <summary>
        /// Call after editing the theme. Edits are live immediately - saving only writes them to
        /// disk - so the editor shows the result while you drag a slider.
        /// </summary>
        public static void MarkChanged()
        {
            EditorUtility.SetDirty(instance);
            Changed.Invoke();

            RepaintAll();
        }

        public static void Save()
        {
            Theme.RemoveNeutralWindows();
            Theme.RemoveUnusedTextures();

            instance.Save(saveAsText: true);
        }

        public static void Replace(Theme theme)
        {
            instance._theme = theme ?? new Theme();

            MarkChanged();
        }

        public static void ExportToFile(string path)
        {
            Theme.RemoveNeutralWindows();
            Theme.RemoveUnusedTextures();

            File.WriteAllText(path, JsonUtility.ToJson(Theme));
        }

        public static bool ImportFromFile(string path)
        {
            try
            {
                var theme = JsonUtility.FromJson<Theme>(File.ReadAllText(path));
                if (theme == null) return false;

                Replace(theme);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void RepaintAll()
        {
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window != null) window.Repaint();
            }
        }
    }
}
