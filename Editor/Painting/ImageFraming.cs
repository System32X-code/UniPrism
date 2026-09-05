using UnityEditor;
using UnityEngine;

namespace UniPrism
{
    /// <summary>
    /// Works out which part of an image a given rect should show.
    /// </summary>
    /// <remarks>
    /// Shared by the painter and the framing preview, deliberately: a preview that approximates
    /// the real framing rather than using it is worse than no preview at all.
    /// </remarks>
    internal static class ImageFraming
    {
        public static void Draw(Rect rect, Texture2D texture, AppearanceSettings settings, bool spanEditor)
        {
            if (texture == null || rect.width <= 0f || rect.height <= 0f) return;

            if (spanEditor && settings.SpanEditor && TryDrawSpanning(rect, texture, settings)) return;

            switch (settings.ImageScaleMode)
            {
                case ImageScaleMode.Stretch:
                    GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill);
                    return;

                case ImageScaleMode.Fit:
                    GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
                    return;
            }

            GUI.DrawTextureWithTexCoords(rect, texture, CoverCoords(rect.width / rect.height, texture, settings));
        }

        /// <summary>
        /// Draws the slice of the image that lies behind this rect, treating the image as one
        /// picture stretched across the whole editor window.
        /// </summary>
        /// <remarks>
        /// The rect is converted to screen space and expressed as a fraction of the main editor
        /// window, then that fraction is applied to the image coordinates the picture would occupy
        /// if it covered the editor. Windows therefore line up into a single continuous image
        /// instead of each showing their own squashed copy.
        /// </remarks>
        private static bool TryDrawSpanning(Rect rect, Texture2D texture, AppearanceSettings settings)
        {
            var editor = EditorGUIUtility.GetMainWindowPosition();
            if (editor.width <= 0f || editor.height <= 0f) return false;

            //Only valid inside an OnGUI scope, which is the only place this is called from.
            var topLeft = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));

            var fractionX = (topLeft.x - editor.x) / editor.width;
            var fractionY = (topLeft.y - editor.y) / editor.height;
            var fractionWidth = rect.width / editor.width;
            var fractionHeight = rect.height / editor.height;

            //A floating window can sit outside the main window entirely; spanning means nothing there.
            if (fractionX < -1f || fractionX > 2f || fractionY < -1f || fractionY > 2f) return false;

            var cover = CoverCoords(editor.width / editor.height, texture, settings);

            var coords = new Rect(
                cover.x + cover.width * fractionX,
                //Texture coordinates run bottom-up, screen coordinates top-down.
                cover.y + cover.height * (1f - fractionY - fractionHeight),
                cover.width * fractionWidth,
                cover.height * fractionHeight);

            GUI.DrawTextureWithTexCoords(rect, texture, coords);

            return true;
        }

        /// <summary>
        /// The image coordinates that fill a rect of the given aspect without distortion, after
        /// zoom and alignment.
        /// </summary>
        private static Rect CoverCoords(float targetAspect, Texture2D texture, AppearanceSettings settings)
        {
            var imageAspect = texture.height <= 0 ? 1f : (float)texture.width / texture.height;

            var width = imageAspect > targetAspect ? targetAspect / imageAspect : 1f;
            var height = imageAspect > targetAspect ? 1f : imageAspect / targetAspect;

            var zoom = settings.ImageZoom;
            width = Mathf.Clamp01(width / zoom);
            height = Mathf.Clamp01(height / zoom);

            var alignment = settings.ImageAlignment;

            return new Rect(
                (1f - width) * alignment.x,
                (1f - height) * (1f - alignment.y),
                width,
                height);
        }
    }
}
