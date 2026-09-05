using System;
using UnityEditor;
using UnityEngine;

namespace UniPrism
{
    /// <summary>
    /// Who made this and where to find it.
    /// </summary>
    internal static class AboutPage
    {
        private const string AvatarAssetPath = "Packages/com.system32x.uniprism/Editor/UI/Author.png";

        private const string AuthorName = "System32X-code";
        private const string GitHubUrl = "https://github.com/System32X-code";
        private const string BilibiliUrl = "https://space.bilibili.com/108742637";
        private const string ProjectUrl = "https://github.com/System32X-code/UniPrism";

        private const float AvatarSize = 96f;

        private static Texture2D _avatar;
        private static bool _avatarLoadAttempted;
        private static string _version;

        public static void Draw()
        {
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawAvatar();

                using (new EditorGUILayout.VerticalScope())
                {
                    GUILayout.Space(4f);

                    EditorGUILayout.LabelField("UniPrism", EditorStyles.largeLabel);
                    EditorGUILayout.LabelField(Version(), EditorStyles.miniLabel);

                    GUILayout.Space(6f);

                    EditorGUILayout.LabelField(AuthorName, EditorStyles.boldLabel);
                }
            }

            EditorGUILayout.Space();

            DrawLink(Loc.Tr("Project page"), ProjectUrl);
            DrawLink(Loc.Tr("GitHub"), GitHubUrl);
            DrawLink(Loc.Tr("Bilibili"), BilibiliUrl);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(Loc.Tr("Licence"), EditorStyles.boldLabel);
            EditorGUILayout.LabelField(Loc.Tr("MIT. Free to use and modify, as long as the notice travels with it."), EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space();

            EditorGUILayout.LabelField(Loc.Tr("Developer"), EditorStyles.boldLabel);

            if (GUILayout.Button(Loc.Tr("Log diagnostics report")))
            {
                //Every way this can fail is silent by design, so the report is the only way to
                //tell an unhooked window from a title that does not match.
                Diagnostics.LogReport();
            }

            EditorGUILayout.LabelField(Loc.Tr("Writes what the painter can actually see to the console."), EditorStyles.wordWrappedMiniLabel);
        }

        private static void DrawAvatar()
        {
            var rect = GUILayoutUtility.GetRect(AvatarSize, AvatarSize, GUILayout.Width(AvatarSize), GUILayout.ExpandWidth(false));

            var avatar = Avatar();
            if (avatar == null)
            {
                //Nothing to fall back to but empty space; an About page is not worth an error.
                return;
            }

            GUI.DrawTexture(rect, avatar, ScaleMode.ScaleToFit);
        }

        private static void DrawLink(string label, string url)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(90f));

                var rect = GUILayoutUtility.GetRect(new GUIContent(url), EditorStyles.linkLabel);

                if (GUI.Button(rect, url, EditorStyles.linkLabel))
                {
                    Application.OpenURL(url);
                }

                EditorGUIUtility.AddCursorRect(rect, MouseCursor.Link);
            }
        }

        private static Texture2D Avatar()
        {
            if (_avatarLoadAttempted) return _avatar;

            _avatarLoadAttempted = true;

            //Loaded by package path, which is how an asset inside a package is addressed.
            _avatar = AssetDatabase.LoadAssetAtPath<Texture2D>(AvatarAssetPath);

            return _avatar;
        }

        private static string Version()
        {
            if (_version != null) return _version;

            try
            {
                //Reads package.json, so the About page cannot drift out of sync with the manifest.
                var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(AboutPage).Assembly);
                _version = package == null ? string.Empty : $"v{package.version}";
            }
            catch (Exception)
            {
                _version = string.Empty;
            }

            return _version;
        }
    }
}
