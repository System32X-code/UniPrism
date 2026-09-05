using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniPrism
{
    /// <summary>
    /// A named set of window appearances plus the images they use, serializable to a single file.
    /// </summary>
    [Serializable]
    internal class Theme
    {
        [SerializeField]
        private string _name;
        [SerializeField]
        private List<WindowAppearance> _windows = new List<WindowAppearance>();
        [SerializeField]
        private List<ThemeTexture> _textures = new List<ThemeTexture>();
        [SerializeField]
        private Palette _palette = new Palette();
        [SerializeField]
        private AppearanceSettings _global = AppearanceSettings.CreateNeutral();

        public string Name
        {
            get => string.IsNullOrEmpty(_name) ? "Default" : _name;
            set => _name = value;
        }

        public Palette Palette => _palette ?? (_palette = new Palette());

        /// <summary>
        /// The look every window starts from. A window only departs from it where it says so.
        /// </summary>
        public AppearanceSettings Global => _global ?? (_global = AppearanceSettings.CreateNeutral());

        public IReadOnlyList<WindowAppearance> Windows => _windows;
        public IReadOnlyList<ThemeTexture> Textures => _textures;

        /// <summary>
        /// Lookup is by window title and happens on every repaint of every window, so it is worth
        /// not walking the list each time. Rebuilt lazily because the lists are what deserializes.
        /// </summary>
        private Dictionary<string, WindowAppearance> _byTitle;

        public WindowAppearance Find(string windowTitle)
        {
            if (string.IsNullOrEmpty(windowTitle)) return null;

            if (_byTitle == null)
            {
                _byTitle = new Dictionary<string, WindowAppearance>(StringComparer.Ordinal);

                foreach (var window in _windows)
                {
                    if (window == null || string.IsNullOrEmpty(window.WindowTitle)) continue;

                    _byTitle[window.WindowTitle] = window;
                }
            }

            return _byTitle.TryGetValue(windowTitle, out var appearance) ? appearance : null;
        }

        public WindowAppearance FindOrCreate(string windowTitle)
        {
            var existing = Find(windowTitle);
            if (existing != null) return existing;

            var appearance = new WindowAppearance(windowTitle);
            _windows.Add(appearance);
            _byTitle = null;

            return appearance;
        }

        public ThemeTexture FindTexture(string textureId)
        {
            if (string.IsNullOrEmpty(textureId)) return null;

            foreach (var texture in _textures)
            {
                if (texture != null && texture.Id == textureId) return texture;
            }

            return null;
        }

        public ThemeTexture AddTextureFromFile(string path)
        {
            var texture = ThemeTexture.FromFile(path);
            if (texture == null) return null;

            _textures.Add(texture);

            return texture;
        }

        public ThemeTexture AddTexture(Texture2D source)
        {
            var texture = ThemeTexture.From(source);
            if (texture == null) return null;

            _textures.Add(texture);

            return texture;
        }

        /// <summary>
        /// Images are stored inline, so an unreferenced one is dead weight in every saved file.
        /// </summary>
        public void RemoveUnusedTextures()
        {
            var used = new HashSet<string>(StringComparer.Ordinal);

            if (Global.HasBackground) used.Add(Global.BackgroundTextureId);

            foreach (var window in _windows)
            {
                if (window != null && window.Settings.HasBackground)
                {
                    used.Add(window.Settings.BackgroundTextureId);
                }
            }

            _textures.RemoveAll(texture => texture == null || !used.Contains(texture.Id));
        }

        public void RemoveNeutralWindows()
        {
            _windows.RemoveAll(window => window == null || window.IsNeutral);
            _byTitle = null;
        }

        public Theme Clone()
        {
            var clone = new Theme { _name = _name };

            foreach (var window in _windows)
            {
                if (window != null) clone._windows.Add(window.Clone());
            }

            //Textures are immutable once encoded, so they can be shared rather than copied.
            clone._textures.AddRange(_textures);
            clone._palette = _palette;
            clone._global = Global.Clone();

            return clone;
        }
    }
}
