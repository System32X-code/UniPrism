using System;
using UnityEngine;

namespace UniPrism
{
    /// <summary>
    /// One window's departure from the theme's global look.
    /// </summary>
    /// <remarks>
    /// Overrides are per section rather than per field. A window that overrides nothing follows
    /// the global settings entirely, which is what makes "change the whole editor at once" work
    /// without having to touch every window afterwards.
    /// </remarks>
    [Serializable]
    internal class WindowAppearance
    {
        [SerializeField]
        private string _windowTitle;
        [SerializeField]
        private bool _overridesBackground;
        [SerializeField]
        private bool _overridesColours;
        [SerializeField]
        private AppearanceSettings _settings;

        public string WindowTitle
        {
            get => _windowTitle;
            set => _windowTitle = value;
        }

        public bool OverridesBackground
        {
            get => _overridesBackground;
            set => _overridesBackground = value;
        }

        public bool OverridesColours
        {
            get => _overridesColours;
            set => _overridesColours = value;
        }

        public AppearanceSettings Settings => _settings ?? (_settings = AppearanceSettings.CreateNeutral());

        public bool IsNeutral => !_overridesBackground && !_overridesColours;

        public WindowAppearance(string windowTitle)
        {
            _windowTitle = windowTitle;
            _overridesBackground = false;
            _overridesColours = false;
            _settings = AppearanceSettings.CreateNeutral();
        }

        public WindowAppearance Clone()
        {
            return new WindowAppearance(_windowTitle)
            {
                _overridesBackground = _overridesBackground,
                _overridesColours = _overridesColours,
                _settings = Settings.Clone()
            };
        }
    }
}
