using System;
using UnityEngine;

namespace UniPrism
{
    /// <summary>
    /// How one editor window should look, keyed by the window's title.
    /// </summary>
    /// <remarks>
    /// Colour fields default to <c>(0,0,0,0)</c> when a theme saved by an older version is loaded,
    /// which would render the window blank. Every getter resolves that to the neutral value
    /// instead, so an older theme keeps looking the way it was authored.
    /// </remarks>
    [Serializable]
    internal class WindowAppearance
    {
        [SerializeField]
        private string _windowTitle;
        [SerializeField]
        private string _backgroundTextureId;
        [SerializeField]
        private Color _backgroundTint;
        [SerializeField]
        private bool _drawOverContent;
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
        private Color _contentTint;

        public string WindowTitle
        {
            get => _windowTitle;
            set => _windowTitle = value;
        }

        public string BackgroundTextureId
        {
            get => _backgroundTextureId;
            set => _backgroundTextureId = value;
        }

        /// <summary>Applied to the background image itself; the alpha is its opacity.</summary>
        public Color BackgroundTint
        {
            get => Neutral(_backgroundTint);
            set => _backgroundTint = value;
        }

        /// <summary>
        /// Draw the image over the window's content rather than under it.
        /// </summary>
        /// <remarks>
        /// Under is the better place and the default. Over is the fallback for a window that
        /// paints a backdrop the tint cannot thin out enough to see through.
        /// </remarks>
        public bool DrawOverContent
        {
            get => _drawOverContent;
            set => _drawOverContent = value;
        }

        /// <summary>
        /// Multiplies <c>GUI.backgroundColor</c> while the window draws: the alpha thins the
        /// window's own backdrop so the image shows through, and the colour recolours its panels.
        /// </summary>
        public Color BackdropTint
        {
            get => Neutral(_backdropTint);
            set => _backdropTint = value;
        }

        /// <summary>
        /// Multiplies <c>GUI.contentColor</c>, which is what recolours text and icons. Separate
        /// from the backdrop, so thinning one does not wash out the other.
        /// </summary>
        public Color ContentTint
        {
            get => Neutral(_contentTint);
            set => _contentTint = value;
        }

        /// <summary>How the image fills the window. Crop is the default and the only mode that
        /// uses <see cref="ImageAlignment"/> and <see cref="ImageZoom"/>.</summary>
        public ImageScaleMode ImageScaleMode
        {
            get => _imageScaleMode;
            set => _imageScaleMode = value;
        }

        /// <summary>Which part of a cropped image stays visible. (0,0) is bottom left.</summary>
        public Vector2 ImageAlignment
        {
            get => new Vector2(Mathf.Clamp01(_imageAlignment.x), Mathf.Clamp01(_imageAlignment.y));
            set => _imageAlignment = value;
        }

        /// <summary>Zero means the field predates zoom, not an image scaled to nothing.</summary>
        public float ImageZoom
        {
            get => _imageZoom <= 0f ? 1f : Mathf.Clamp(_imageZoom, 1f, 8f);
            set => _imageZoom = value;
        }

        /// <summary>
        /// Where the backdrop colour comes from. Custom keeps whatever is in
        /// <see cref="BackdropTint"/>; any other slot takes its RGB from the theme palette and
        /// keeps this window's own opacity.
        /// </summary>
        public PaletteSlot BackdropSlot
        {
            get => _backdropSlot;
            set => _backdropSlot = value;
        }

        public bool HasBackground => !string.IsNullOrEmpty(_backgroundTextureId);

        public bool IsNeutral => !HasBackground
            && BackdropTint == Color.white
            && ContentTint == Color.white
            && _backdropSlot == PaletteSlot.Custom;

        /// <summary>
        /// The backdrop colour actually used, once the palette has had its say.
        /// </summary>
        public Color ResolveBackdrop(Palette palette)
        {
            if (_backdropSlot == PaletteSlot.Custom || palette == null) return BackdropTint;

            var slotColour = palette.Resolve(_backdropSlot);

            //Opacity stays per window: two windows can share a colour and differ in how much of
            //their own backdrop they let through.
            return new Color(slotColour.r, slotColour.g, slotColour.b, BackdropTint.a);
        }

        public WindowAppearance(string windowTitle)
        {
            _windowTitle = windowTitle;
            _backgroundTextureId = string.Empty;
            _backgroundTint = Color.white;
            _drawOverContent = false;
            _backdropTint = Color.white;
            _contentTint = Color.white;
            _imageScaleMode = ImageScaleMode.Crop;
            _imageAlignment = new Vector2(0.5f, 0.5f);
            _imageZoom = 1f;
            _backdropSlot = PaletteSlot.Custom;
        }

        public WindowAppearance Clone()
        {
            return new WindowAppearance(_windowTitle)
            {
                _backgroundTextureId = _backgroundTextureId,
                _backgroundTint = _backgroundTint,
                _drawOverContent = _drawOverContent,
                _backdropTint = _backdropTint,
                _contentTint = _contentTint,
                _imageScaleMode = _imageScaleMode,
                _imageAlignment = _imageAlignment,
                _imageZoom = _imageZoom,
                _backdropSlot = _backdropSlot
            };
        }

        private static Color Neutral(Color colour) => colour == default ? Color.white : colour;
    }
}
