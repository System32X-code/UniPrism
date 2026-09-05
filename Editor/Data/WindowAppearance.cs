using System;
using UnityEngine;

namespace Prism
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

        public bool HasBackground => !string.IsNullOrEmpty(_backgroundTextureId);

        public bool IsNeutral => !HasBackground
            && BackdropTint == Color.white
            && ContentTint == Color.white;

        public WindowAppearance(string windowTitle)
        {
            _windowTitle = windowTitle;
            _backgroundTextureId = string.Empty;
            _backgroundTint = Color.white;
            _drawOverContent = false;
            _backdropTint = Color.white;
            _contentTint = Color.white;
        }

        public WindowAppearance Clone()
        {
            return new WindowAppearance(_windowTitle)
            {
                _backgroundTextureId = _backgroundTextureId,
                _backgroundTint = _backgroundTint,
                _drawOverContent = _drawOverContent,
                _backdropTint = _backdropTint,
                _contentTint = _contentTint
            };
        }

        private static Color Neutral(Color colour) => colour == default ? Color.white : colour;
    }
}
