using UnityEngine;

namespace UniPrism
{
    /// <summary>
    /// What a window is actually painted with, after global settings and the window's own
    /// overrides have been reconciled and the palette resolved.
    /// </summary>
    /// <remarks>
    /// Resolved in one place so the two painters cannot disagree, and so the painters never have
    /// to know that overrides or a palette exist. Colours are resolved here, not looked up during
    /// drawing, which keeps a repaint free of dictionary work.
    /// </remarks>
    internal readonly struct EffectiveAppearance
    {
        public readonly AppearanceSettings Background;
        public readonly Color BackdropTint;
        public readonly Color ContentTint;
        public readonly bool TintIcons;
        public readonly Color ChromeTint;

        private EffectiveAppearance(AppearanceSettings background, Color backdropTint, Color contentTint, bool tintIcons, Color chromeTint)
        {
            Background = background;
            BackdropTint = backdropTint;
            ContentTint = contentTint;
            TintIcons = tintIcons;
            ChromeTint = chromeTint;
        }

        public bool HasBackground => Background != null && Background.HasBackground;

        public bool IsNeutral => !HasBackground
            && BackdropTint == Color.white
            && ContentTint == Color.white
            && ChromeTint.a <= 0f;

        public static EffectiveAppearance Resolve(Theme theme, string windowTitle)
        {
            if (theme == null) return default;

            var global = theme.Global;
            var window = theme.Find(windowTitle);

            var background = window != null && window.OverridesBackground ? window.Settings : global;
            var colours = window != null && window.OverridesColours ? window.Settings : global;

            var palette = theme.Palette;

            return new EffectiveAppearance(
                background,
                colours.ResolveBackdrop(palette),
                colours.ResolveContent(palette),
                colours.TintIcons,
                colours.ResolveChrome(palette));
        }
    }
}
