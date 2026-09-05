using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniPrism
{
    internal enum UniPrismLanguage
    {
        English,
        Chinese
    }

    /// <summary>
    /// String table for UniPrism's own window.
    /// </summary>
    /// <remarks>
    /// Unity's editor localization (<c>L10n.Tr</c> with .po files) follows the editor-wide
    /// language preference, which is not what an in-window toggle needs. Keys are the English
    /// source strings, gettext style, so an untranslated string still reads correctly.
    /// </remarks>
    internal static class Loc
    {
        private const string PreferenceKey = "UniPrism.Language";

        private static UniPrismLanguage? _current;

        public static UniPrismLanguage Current
        {
            get
            {
                if (_current is null)
                {
                    _current = (UniPrismLanguage)EditorPrefs.GetInt(PreferenceKey, (int)SystemDefault());
                }

                return _current.Value;
            }
            set
            {
                if (_current == value) return;

                _current = value;
                EditorPrefs.SetInt(PreferenceKey, (int)value);
            }
        }

        /// <summary>Label of the button, naming the language it switches to.</summary>
        public static string ToggleLabel => Current is UniPrismLanguage.Chinese ? "English" : "中文";

        public static void Toggle()
        {
            Current = Current is UniPrismLanguage.Chinese ? UniPrismLanguage.English : UniPrismLanguage.Chinese;
        }

        public static string Tr(string source)
        {
            if (Current is UniPrismLanguage.English) return source;

            return _chinese.TryGetValue(source, out var translated) ? translated : source;
        }

        private static UniPrismLanguage SystemDefault()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                    return UniPrismLanguage.Chinese;
                default:
                    return UniPrismLanguage.English;
            }
        }

        private static readonly Dictionary<string, string> _chinese = new Dictionary<string, string>
        {
            { "Theme", "主题" },
            { "About", "关于" },
            { "Image opacity", "图片不透明度" },
            { "Backdrop opacity", "底板不透明度" },
            { "Text opacity", "文字不透明度" },
            { "Project page", "项目主页" },
            { "GitHub", "GitHub" },
            { "Bilibili", "哔哩哔哩" },
            { "Licence", "许可证" },
            { "MIT. Free to use and modify, as long as the notice travels with it.", "MIT。可以自由使用和修改，只要声明跟着代码走。" },
            { "Grew out of debugging UniSkin by piti6, also MIT.", "源于对 piti6 的 UniSkin 的排查，那个项目同为 MIT。" },
            { "Save", "保存" },
            { "Export", "导出" },
            { "Import", "导入" },
            { "Reset", "重置" },
            { "Window", "窗口" },
            { "Background", "背景" },
            { "Image", "图片" },
            { "Image tint", "图片染色" },
            { "Draw over content", "绘制在内容之上" },
            { "Colours", "配色" },
            { "Backdrop tint", "底板染色" },
            { "Text and icon tint", "文字与图标染色" },
            { "Reset this window", "重置此窗口" },
            { "Pick a window to style.", "选择一个要设置的窗口。" },
            { "No editor windows found.", "没有找到编辑器窗口。" },
            { "Export theme", "导出主题" },
            { "Import theme", "导入主题" },
            { "Could not read that theme file.", "无法读取该主题文件。" },
            { "Reset every window in this theme?", "重置该主题中的所有窗口？" },
            { "Yes", "是" },
            { "No", "否" },
            { "UniPrism is inactive: ", "UniPrism 未生效：" },
            { "Lower the backdrop tint's alpha to thin out the window's own backdrop so the image shows through. Text stays legible because it is tinted separately.",
              "调低底板染色的 alpha，窗口自己那层底会变薄，图片就透出来了。文字由另一路染色控制，保持清晰。" },
        };
    }
}
