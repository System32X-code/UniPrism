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
            { "Global", "全局" },
            { "Per window", "按窗口" },
            { "Window backdrop", "窗口底板" },
            { "Text and icons", "文字与图标" },
            { "Text tint", "文字染色" },
            { "One image across the editor", "整张图铺满编辑器" },
            { "Override the global background", "覆盖全局背景" },
            { "Override the global colours", "覆盖全局配色" },
            { "Follow global again", "恢复跟随全局" },
            { "Everything here applies to every window. Give a single window something different on the Per window page.", "这一页的设置对所有窗口生效。想让某个窗口不一样，去「按窗口」页设置。" },
            { "Anything set to a palette colour follows it, so editing one colour here recolours everything using it at once.", "凡是选了调色板颜色的地方都会跟着它走，所以在这里改一个颜色，用到它的地方会一起变。" },
            { "With both switches off this window simply follows the global look.", "两个开关都关掉，这个窗口就完全跟随全局。" },
            { "The image is laid over the whole editor and each window shows the part behind it, instead of every window scaling its own copy.", "整张图铺在编辑器上，每个窗口只显示自己背后的那一块，而不是每个窗口各自缩放一份完整的图。" },
            { "Text", "文字" },
            { "Tint icons too", "同时染图标" },
            { "The backdrop thins out to reveal a background image. Text is tinted through the styles, so icons keep their own colours unless you ask for them. The frame is the dock's tab strip, washed over rather than tinted.", "底板变薄才能露出背景图；文字是通过样式上色的，所以图标不受影响，除非你勾上“同时染图标”；边框指停靠区的标签栏，是覆色而不是染色。" },
            { "Browse...", "浏览..." },
            { "Choose an image", "选择图片" },
            { "Images", "图片" },
            { "That file could not be read as an image.", "无法把该文件读取为图片。" },
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
            { "Window frame", "窗口边框" },
            { "Frame strength", "边框染色强度" },
            { "Frame tint", "边框颜色" },
            { "The tab strip and border belong to the dock, not the window, so they are washed over rather than tinted. Strength 0 leaves them untouched.", "标签栏和边框属于停靠区而不是窗口本身，所以是覆上一层颜色而不是染色。强度为 0 则完全不动。" },
            { "Lower the backdrop tint's alpha to thin out the window's own backdrop so the image shows through. Text stays legible because it is tinted separately.",
              "调低底板不透明度，窗口自己那层底会变薄，图片就透出来了。文字由另一路染色控制，保持清晰。" },

            //Palette
            { "Palette", "调色板" },
            { "Primary", "一级色" },
            { "Secondary", "二级色" },
            { "Tertiary", "三级色" },
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
            { "Developer", "开发者调试" },
            { "Log diagnostics report", "输出诊断报告" },
            { "Writes what the painter can actually see to the console.", "把绘制器实际看到的状态打印到控制台。" },
            { "Licence", "许可证" },
            { "UniPrism is inactive: ", "UniPrism 未生效：" },
        };

        private static readonly Dictionary<string, string> _japanese = new Dictionary<string, string>
        {
            //Toolbar
            { "Global", "グローバル" },
            { "Per window", "ウィンドウ別" },
            { "Window backdrop", "ウィンドウの背景パネル" },
            { "Text and icons", "テキストとアイコン" },
            { "Text tint", "テキストの色味" },
            { "One image across the editor", "エディター全体に1枚の画像" },
            { "Override the global background", "グローバルの背景を上書き" },
            { "Override the global colours", "グローバルのカラーを上書き" },
            { "Follow global again", "グローバルに戻す" },
            { "Everything here applies to every window. Give a single window something different on the Per window page.", "ここの設定はすべてのウィンドウに適用されます。個別に変えたい場合は「ウィンドウ別」ページで設定してください。" },
            { "Anything set to a palette colour follows it, so editing one colour here recolours everything using it at once.", "パレットの色を参照している箇所はその色に追従するため、ここで1色変えるだけでまとめて配色が変わります。" },
            { "With both switches off this window simply follows the global look.", "両方のスイッチをオフにすると、このウィンドウはグローバル設定に従います。" },
            { "The image is laid over the whole editor and each window shows the part behind it, instead of every window scaling its own copy.", "画像はエディター全体に敷かれ、各ウィンドウはその背後にあたる部分だけを表示します。ウィンドウごとに縮小した複製が並ぶことはありません。" },
            { "Text", "テキスト" },
            { "Tint icons too", "アイコンも色付け" },
            { "The backdrop thins out to reveal a background image. Text is tinted through the styles, so icons keep their own colours unless you ask for them. The frame is the dock's tab strip, washed over rather than tinted.", "背景パネルが薄くなることで背景画像が透けます。テキストはスタイル経由で色付けされるため、「アイコンも色付け」を有効にしない限りアイコンは元の色のままです。枠はドックのタブ帯で、色調補正ではなく上から重ねています。" },
            { "Browse...", "参照..." },
            { "Choose an image", "画像を選択" },
            { "Images", "画像" },
            { "That file could not be read as an image.", "そのファイルを画像として読み込めませんでした。" },
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
            { "Window frame", "ウィンドウ枠" },
            { "Frame strength", "枠の強さ" },
            { "Frame tint", "枠の色" },
            { "The tab strip and border belong to the dock, not the window, so they are washed over rather than tinted. Strength 0 leaves them untouched.", "タブと枠はウィンドウ自体ではなくドック側が描画するため、色調補正ではなく上から重ねています。強さ 0 では何も変わりません。" },
            { "Lower the backdrop tint's alpha to thin out the window's own backdrop so the image shows through. Text stays legible because it is tinted separately.",
              "背景パネルの不透明度を下げると、ウィンドウ自身のパネルが薄くなり画像が透けて見えます。テキストは別系統で色付けされるため読みやすさは保たれます。" },

            //Palette
            { "Palette", "パレット" },
            { "Primary", "プライマリ" },
            { "Secondary", "セカンダリ" },
            { "Tertiary", "ターシャリ" },
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
            { "Developer", "開発者向け" },
            { "Log diagnostics report", "診断レポートを出力" },
            { "Writes what the painter can actually see to the console.", "ペインターが実際に認識している状態をコンソールに出力します。" },
            { "Licence", "ライセンス" },
            { "UniPrism is inactive: ", "UniPrism は動作していません: " },
        };
    }
}
