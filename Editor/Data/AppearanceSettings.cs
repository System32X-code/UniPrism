using System;
using UnityEngine;

namespace UniPrism
{
    /// <summary>
    /// One complete look: a background image and the three colours a window is painted with.
    /// </summary>
    /// <remarks>
    /// The same block serves as the theme's global default and as a window's own override, so
    /// there is one definition of what a look consists of rather than two that drift apart.
    /// <para/>
    /// Colour fields deserialize to <c>(0,0,0,0)</c> in a theme saved before they existed, which
    /// would paint a window black instead of leaving it alone, so each getter resolves that to its
    /// neutral value.
    /// </remarks>
    [Serializable]
    internal class AppearanceSettings
    {
        [SerializeField]
        private string _backgroundTextureId;
        [SerializeField]
        private Color _backgroundTint;
        [SerializeField]
        private bool _drawOverContent;
        [SerializeField]
        private bool _spanEditor;
        [SerializeField]
        private ImageScaleMode _imageScaleMode;
        [SerializeField]
        private Vector2 _imageAlignment;
        [SerializeField]
        private float _imageZoom;

        [SerializeField]
        private PaletteSlot _backdropSlot;
        [SerializeField]
        private Color _backdropTint;
        [SerializeField]
        private PaletteSlot _contentSlot;
        [SerializeField]
        private Color _contentTint;
        [SerializeField]
        private bool _tintIcons;
        [SerializeField]
        private PaletteSlot _chromeSlot;
        [SerializeField]
        private Color _chromeTint;

        public string BackgroundTextureId
        {
            get => _backgroundTextureId;
            set => _backgroundTextureId = value;
        }

        public bool HasBackground => !string.IsNullOrEmpty(_backgroundTextureId);

        /// <summary>Applied to the image itself; the alpha is its opacity.</summary>
        public Color BackgroundTint
        {
            get => Neutral(_backgroundTint);
            set => _backgroundTint = value;
        }

        public bool DrawOverContent
        {
            get => _drawOverContent;
            set => _drawOverContent = value;
        }

        /// <summary>
        /// Treat the image as one picture laid across the whole editor, with each window showing
        /// the part of it that falls behind that window.
        /// </summary>
        /// <remarks>
        /// Without this every window scales the whole image into itself, so a wide photo appears
        /// once per window, squashed differently each time. With it there is a single picture and
        /// the windows are holes onto it, which is what a desktop wallpaper does.
        /// </remarks>
        public bool SpanEditor
        {
            get => _spanEditor;
            set => _spanEditor = value;
        }

        public ImageScaleMode ImageScaleMode
        {
            get => _imageScaleMode;
            set => _imageScaleMode = value;
        }

        public Vector2 ImageAlignment
        {
            get => new Vector2(Mathf.Clamp01(_imageAlignment.x), Mathf.Clamp01(_imageAlignment.y));
            set => _imageAlignment = value;
        }

        /// <summary>Zero means the field predates zoom, not an image scaled away to nothing.</summary>
        public float ImageZoom
        {
            get => _imageZoom <= 0f ? 1f : Mathf.Clamp(_imageZoom, 1f, 8f);
            set => _imageZoom = value;
        }

        public PaletteSlot BackdropSlot
        {
            get => _backdropSlot;
            set => _backdropSlot = value;
        }

        /// <summary>Multiplies the window's own panels; the alpha thins them out.</summary>
        public Color BackdropTint
        {
            get => Neutral(_backdropTint);
            set => _backdropTint = value;
        }

        public PaletteSlot ContentSlot
        {
            get => _contentSlot;
            set => _contentSlot = value;
        }

        /// <summary>Multiplies text and icons, on a separate channel from the backdrop.</summary>
        public Color ContentTint
        {
            get => Neutral(_contentTint);
            set => _contentTint = value;
        }

        /// <summary>
        /// Put icons on the text tint as well.
        /// </summary>
        /// <remarks>
        /// Off by default, and off means text is tinted through the styles, which icons do not
        /// read. On falls back to <c>GUI.contentColor</c>, the shared multiplier that catches both
        /// - the only way to reach icons at all, at the cost of no longer being able to leave them
        /// alone.
        /// </remarks>
        public bool TintIcons
        {
            get => _tintIcons;
            set => _tintIcons = value;
        }

        public PaletteSlot ChromeSlot
        {
            get => _chromeSlot;
            set => _chromeSlot = value;
        }

        /// <summary>Washes the dock's tab strip and border. Alpha zero leaves them alone.</summary>
        public Color ChromeTint
        {
            get => _chromeTint;
            set => _chromeTint = value;
        }

        public static AppearanceSettings CreateNeutral()
        {
            return new AppearanceSettings
            {
                _backgroundTextureId = string.Empty,
                _backgroundTint = Color.white,
                _drawOverContent = false,
                _spanEditor = false,
                _imageScaleMode = ImageScaleMode.Crop,
                _imageAlignment = new Vector2(0.5f, 0.5f),
                _imageZoom = 1f,
                _backdropSlot = PaletteSlot.Custom,
                _backdropTint = Color.white,
                _contentSlot = PaletteSlot.Custom,
                _contentTint = Color.white,
                _tintIcons = false,
                _chromeSlot = PaletteSlot.Custom,
                _chromeTint = new Color(1f, 1f, 1f, 0f)
            };
        }

        public bool IsNeutral => !HasBackground
            && BackdropTint == Color.white && _backdropSlot == PaletteSlot.Custom
            && ContentTint == Color.white && _contentSlot == PaletteSlot.Custom
            && _chromeTint.a <= 0f && _chromeSlot == PaletteSlot.Custom;

        /// <summary>
        /// A slot other than Custom takes its colour from the palette but keeps this block's own
        /// alpha, so several windows can share a colour and still differ in strength.
        /// </summary>
        public Color ResolveBackdrop(Palette palette) => Resolve(palette, _backdropSlot, BackdropTint);

        public Color ResolveContent(Palette palette) => Resolve(palette, _contentSlot, ContentTint);

        public Color ResolveChrome(Palette palette) => Resolve(palette, _chromeSlot, _chromeTint);

        private static Color Resolve(Palette palette, PaletteSlot slot, Color own)
        {
            if (slot == PaletteSlot.Custom || palette == null) return own;

            var slotColour = palette.Resolve(slot);

            return new Color(slotColour.r, slotColour.g, slotColour.b, own.a);
        }

        public AppearanceSettings Clone()
        {
            return (AppearanceSettings)MemberwiseClone();
        }

        private static Color Neutral(Color colour) => colour == default ? Color.white : colour;
    }
}
