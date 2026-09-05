using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniPrism
{
    internal enum PrismLanguage
    {
        English = 0,
        Chinese = 1,
        Japanese = 2
    }

    /// <summary>
    /// String table for UniPrism's own window.
    /// </summary>
    /// <remarks>
    /// Unity's editor localization (<c>L10n.Tr</c> with .po files) follows the editor-wide
    /// language preference, which is not what an in-window picker needs. Keys are the English
    /// source strings, gettext style, so a string missing from a table still reads correctly at
    /// the call site rather than showing an identifier.
    /// </remarks>
    internal static class Loc
    {
        private const string PreferenceKey = "UniPrism.Language";

        /// <summary>Shown in the picker, each in its own language.</summary>
        public static readonly string[] LanguageNames = { "English", "中文", "日本語" };

        private static PrismLanguage? _current;

        public static PrismLanguage Current
        {
            get
            {
                if (_current is null)
                {
                    _current = (PrismLanguage)EditorPrefs.GetInt(PreferenceKey, (int)SystemDefault());
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

        public static string Tr(string source)
        {
            switch (Current)
            {
                case PrismLanguage.Chinese:
                    return Lookup(_chinese, source);
                case PrismLanguage.Japanese:
                    return Lookup(_japanese, source);
                default:
                    return source;
            }
        }

        private static string Lookup(IReadOnlyDictionary<string, string> table, string source)
        {
            return table.TryGetValue(source, out var translated) ? translated : source;
        }

        private static PrismLanguage SystemDefault()
        {
            switch (Application.systemLanguage)
            {
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                    return PrismLanguage.Chinese;
                case SystemLanguage.Japanese:
                    return PrismLanguage.Japanese;
                default:
                    return PrismLanguage.English;
            }
        }

        private static readonly Dictionary<string, string> _chinese = new Dictionary<string, string>
        {
            //Toolbar
            { "Theme", "主题" },
            { "Save", "保存" },
            { "Export", "导出" },
            { "Import", "导入" },
            { "Reset", "重置" },
            { "About", "关于" },

            //Window selection
            { "Window", "窗口" },
            { "Pick a window to style.", "选择一个要设置的窗口。" },
            { "No editor windows found.", "没有找到编辑器窗口。" },

            //Background
            { "Background", "背景" },
            { "Image", "图片" },
            { "Framing...", "构图..." },
            { "Image opacity", "图片不透明度" },
            { "Image tint", "图片染色" },
            { "Draw over content", "绘制在内容之上" },

            //Framing window
            { "Image settings", "图片设置" },
            { "Scale mode", "缩放模式" },
            { "Crop", "裁切" },
            { "Fit", "适应" },
            { "Stretch", "拉伸" },
            { "Zoom", "缩放" },
            { "Horizontal", "水平位置" },
            { "Vertical", "垂直位置" },
            { "Centre", "居中复位" },
            { "This window has no background image.", "该窗口还没有设置背景图。" },
            { "The image could not be decoded.", "图片无法解码。" },
            { "Fit and Stretch use the whole image, so zoom and alignment only apply to Crop.",
              "「适应」和「拉伸」会用到整张图，所以缩放和位置只对「裁切」生效。" },

            //Colours
            { "Colours", "配色" },
            { "Colour source", "颜色来源" },
            { "Custom", "自定义" },
            { "Backdrop opacity", "底板不透明度" },
            { "Backdrop tint", "底板染色" },
            { "Text opacity", "文字不透明度" },
            { "Text and icon tint", "文字与图标染色" },
            { "Reset this window", "重置此窗口" },
            { "Lower the backdrop tint's alpha to thin out the window's own backdrop so the image shows through. Text stays legible because it is tinted separately.",
              "调低底板不透明度，窗口自己那层底会变薄，图片就透出来了。文字由另一路染色控制，保持清晰。" },

            //Palette
            { "Palette", "调色板" },
            { "Primary", "一级色" },
            { "Secondary", "二级色" },
            { "Accent", "撞色" },
            { "Apply Primary to all windows", "把一级色应用到所有窗口" },
            { "Unlink all", "全部解除关联" },
            { "Point windows at a palette slot and editing that colour recolours all of them at once. Put one group on Primary and another on Accent for contrast.",
              "让窗口指向调色板的某个色位，改那个颜色就能一次性改掉所有关联窗口。一组用一级色、另一组用撞色，就能做出对比。" },

            //Dialogs
            { "Export theme", "导出主题" },
            { "Import theme", "导入主题" },
            { "Could not read that theme file.", "无法读取该主题文件。" },
            { "Reset every window in this theme?", "重置该主题中的所有窗口？" },
            { "Yes", "是" },
            { "No", "否" },

            //About
            { "Project page", "项目主页" },
            { "GitHub", "GitHub" },
            { "Bilibili", "哔哩哔哩" },
            { "Licence", "许可证" },
            { "UniPrism is inactive: ", "UniPrism 未生效：" },
        };

        private static readonly Dictionary<string, string> _japanese = new Dictionary<string, string>
        {
            //Toolbar
            { "Theme", "テーマ" },
            { "Save", "保存" },
            { "Export", "エクスポート" },
            { "Import", "インポート" },
            { "Reset", "リセット" },
            { "About", "情報" },

            //Window selection
            { "Window", "ウィンドウ" },
            { "Pick a window to style.", "設定するウィンドウを選択してください。" },
            { "No editor windows found.", "エディターウィンドウが見つかりません。" },

            //Background
            { "Background", "背景" },
            { "Image", "画像" },
            { "Framing...", "調整..." },
            { "Image opacity", "画像の不透明度" },
            { "Image tint", "画像の色味" },
            { "Draw over content", "コンテンツの上に描画" },

            //Framing window
            { "Image settings", "画像設定" },
            { "Scale mode", "スケールモード" },
            { "Crop", "切り抜き" },
            { "Fit", "全体を表示" },
            { "Stretch", "引き伸ばし" },
            { "Zoom", "ズーム" },
            { "Horizontal", "水平位置" },
            { "Vertical", "垂直位置" },
            { "Centre", "中央に戻す" },
            { "This window has no background image.", "このウィンドウには背景画像が設定されていません。" },
            { "The image could not be decoded.", "画像を読み込めませんでした。" },
            { "Fit and Stretch use the whole image, so zoom and alignment only apply to Crop.",
              "「全体を表示」と「引き伸ばし」は画像全体を使うため、ズームと位置は「切り抜き」にのみ適用されます。" },

            //Colours
            { "Colours", "カラー" },
            { "Colour source", "カラーの参照元" },
            { "Custom", "カスタム" },
            { "Backdrop opacity", "背景パネルの不透明度" },
            { "Backdrop tint", "背景パネルの色味" },
            { "Text opacity", "テキストの不透明度" },
            { "Text and icon tint", "テキストとアイコンの色味" },
            { "Reset this window", "このウィンドウをリセット" },
            { "Lower the backdrop tint's alpha to thin out the window's own backdrop so the image shows through. Text stays legible because it is tinted separately.",
              "背景パネルの不透明度を下げると、ウィンドウ自身のパネルが薄くなり画像が透けて見えます。テキストは別系統で色付けされるため読みやすさは保たれます。" },

            //Palette
            { "Palette", "パレット" },
            { "Primary", "プライマリ" },
            { "Secondary", "セカンダリ" },
            { "Accent", "アクセント" },
            { "Apply Primary to all windows", "プライマリをすべてのウィンドウに適用" },
            { "Unlink all", "すべての参照を解除" },
            { "Point windows at a palette slot and editing that colour recolours all of them at once. Put one group on Primary and another on Accent for contrast.",
              "ウィンドウをパレットの色に紐づけておくと、その色を変えるだけでまとめて配色を変更できます。一部をプライマリ、別の一部をアクセントにするとコントラストが付きます。" },

            //Dialogs
            { "Export theme", "テーマをエクスポート" },
            { "Import theme", "テーマをインポート" },
            { "Could not read that theme file.", "そのテーマファイルを読み込めませんでした。" },
            { "Reset every window in this theme?", "このテーマのすべてのウィンドウをリセットしますか？" },
            { "Yes", "はい" },
            { "No", "いいえ" },

            //About
            { "Project page", "プロジェクトページ" },
            { "GitHub", "GitHub" },
            { "Bilibili", "bilibili" },
            { "Licence", "ライセンス" },
            { "UniPrism is inactive: ", "UniPrism は動作していません: " },
        };
    }
}
