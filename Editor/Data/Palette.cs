using System;
using UnityEngine;

namespace UniPrism
{
    /// <summary>
    /// Which palette colour a window takes its backdrop from.
    /// </summary>
    internal enum PaletteSlot
    {
        Custom = 0,
        Primary = 1,
        Secondary = 2,
        Tertiary = 3
    }

    /// <summary>
    /// How a background image fills its window.
    /// </summary>
    internal enum ImageScaleMode
    {
        /// <summary>Fills the window, cropping whatever does not fit. Honours zoom and alignment.</summary>
        Crop = 0,
        /// <summary>Fits entirely inside the window, letterboxed.</summary>
        Fit = 1,
        /// <summary>Stretched to the window, ignoring the image's aspect ratio.</summary>
        Stretch = 2
    }

    /// <summary>
    /// Three colours a whole theme can be built from, so windows can be recoloured together
    /// instead of one at a time.
    /// </summary>
    /// <remarks>
    /// A window points at a slot rather than copying the colour, so editing the palette repaints
    /// every window using it. Slots are also what makes contrast possible: put one group of
    /// windows on Primary and another on Accent and the pairing stays consistent when either is
    /// changed later.
    /// </remarks>
    [Serializable]
    internal class Palette
    {
        [SerializeField]
        private Color _primary;
        [SerializeField]
        private Color _secondary;
        [SerializeField]
        private Color _tertiary;

        //A theme saved before the palette existed deserializes to (0,0,0,0), which would paint
        //every window black rather than leave it alone.
        public Color Primary
        {
            get => Resolve(_primary);
            set => _primary = value;
        }

        public Color Secondary
        {
            get => Resolve(_secondary);
            set => _secondary = value;
        }

        public Color Tertiary
        {
            get => Resolve(_tertiary);
            set => _tertiary = value;
        }

        public Color Resolve(PaletteSlot slot)
        {
            switch (slot)
            {
                case PaletteSlot.Primary: return Primary;
                case PaletteSlot.Secondary: return Secondary;
                case PaletteSlot.Tertiary: return Tertiary;
                default: return Color.white;
            }
        }

        private static Color Resolve(Color colour) => colour == default ? Color.white : colour;
    }
}
