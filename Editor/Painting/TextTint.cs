using System.Collections.Generic;
using UnityEngine;

namespace UniPrism
{
    /// <summary>
    /// Colours text without colouring icons, by tinting the styles' own text colours instead of
    /// using <c>GUI.contentColor</c>.
    /// </summary>
    /// <remarks>
    /// IMGUI puts text and icons on the same multiplier, so <c>GUI.contentColor</c> cannot
    /// separate them. Text has a second source that icons do not use -
    /// <c>GUIStyle.normal.textColor</c> and its seven siblings - and tinting those colours text
    /// alone.
    /// <para/>
    /// That this works at all is worth recording, because the neighbouring fields do not: on
    /// current Unity versions a style's <c>background</c> can be written, survives the whole
    /// repaint on the very object the IMGUI debugger names, and the editor still draws the
    /// original. Text colour is still read. The two are not interchangeable, and assuming style
    /// fields are uniformly dead would have left this on the table.
    /// <para/>
    /// Runs inside the window's own OnGUI, past <c>ResetGUIState</c>, which is the only point
    /// where <c>GUI.skin</c> is the real editor skin rather than whatever the previous view left.
    /// </remarks>
    internal static class TextTint
    {
        private static readonly List<StateBackup> _backups = new List<StateBackup>();

        private static bool _applied;

        /// <summary>
        /// Multiplies every style's text colours by the tint. Must be paired with
        /// <see cref="Restore"/> in a finally: these styles are shared by the whole editor.
        /// </summary>
        public static void Apply(Color tint)
        {
            if (_applied) return;

            var skin = GUI.skin;
            if (skin == null) return;

            _applied = true;

            TintStyle(skin.box, tint);
            TintStyle(skin.button, tint);
            TintStyle(skin.toggle, tint);
            TintStyle(skin.label, tint);
            TintStyle(skin.textField, tint);
            TintStyle(skin.textArea, tint);
            TintStyle(skin.window, tint);
            TintStyle(skin.horizontalSlider, tint);
            TintStyle(skin.verticalSlider, tint);
            TintStyle(skin.scrollView, tint);

            var customStyles = skin.customStyles;
            if (customStyles == null) return;

            foreach (var style in customStyles)
            {
                TintStyle(style, tint);
            }
        }

        public static void Restore()
        {
            if (!_applied) return;

            //Reverse order so a style reachable twice ends on the value it started with.
            for (var i = _backups.Count - 1; i >= 0; i--)
            {
                _backups[i].Restore();
            }

            _backups.Clear();
            _applied = false;
        }

        private static void TintStyle(GUIStyle style, Color tint)
        {
            if (style == null) return;

            TintState(style.normal, tint);
            TintState(style.hover, tint);
            TintState(style.active, tint);
            TintState(style.focused, tint);
            TintState(style.onNormal, tint);
            TintState(style.onHover, tint);
            TintState(style.onActive, tint);
            TintState(style.onFocused, tint);
        }

        private static void TintState(GUIStyleState state, Color tint)
        {
            if (state == null) return;

            var original = state.textColor;

            _backups.Add(new StateBackup(state, original));

            //Multiplied, not replaced, so styles that differ from each other still differ.
            state.textColor = original * tint;
        }

        private readonly struct StateBackup
        {
            private readonly GUIStyleState _state;
            private readonly Color _textColor;

            public StateBackup(GUIStyleState state, Color textColor)
            {
                _state = state;
                _textColor = textColor;
            }

            public void Restore()
            {
                _state.textColor = _textColor;
            }
        }
    }
}
