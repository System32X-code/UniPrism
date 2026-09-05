using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UniPrism
{
    /// <summary>
    /// Reports what the painter can actually see.
    /// </summary>
    /// <remarks>
    /// Every way this can fail is silent by design - a host that is not hooked, a title that does
    /// not match, an image that did not decode - so without a report there is no way to tell them
    /// apart from "nothing happened".
    /// </remarks>
    internal static class Diagnostics
    {
        [MenuItem("Window/UniPrism Diagnostics")]
        private static void LogReport()
        {
            var report = new StringBuilder();
            report.AppendLine($"UniPrism diagnostics - Unity {Application.unityVersion}");
            report.AppendLine();

            report.AppendLine(HostViewBridge.IsAvailable
                ? "Host bridge: available"
                : $"Host bridge: UNAVAILABLE - {HostViewBridge.UnavailableReason}");

            report.AppendLine($"pixelsPerPoint: {EditorGUIUtility.pixelsPerPoint}");
            report.AppendLine();

            var theme = ThemeStore.Theme;
            report.AppendLine($"Theme '{theme.Name}': {theme.Windows.Count} window(s), {theme.Textures.Count} image(s)");

            foreach (var appearance in theme.Windows)
            {
                if (appearance == null) continue;

                var texture = theme.FindTexture(appearance.BackgroundTextureId);
                var image = !appearance.HasBackground
                    ? "none"
                    : texture == null
                        ? "MISSING from theme"
                        : $"{texture.ByteCount} byte(s), decodes to {(texture.Texture == null ? "<null>" : $"{texture.Texture.width}x{texture.Texture.height}")}";

                report.AppendLine($"    [{appearance.WindowTitle}] image: {image}"
                    + $", over content: {appearance.DrawOverContent}"
                    + $", backdrop: {appearance.BackdropTint}, content: {appearance.ContentTint}");
            }

            report.AppendLine();

            var hooks = WindowPainter.DescribeHooks().ToArray();
            report.AppendLine($"Hooked hosts: {hooks.Length}");

            foreach (var hook in hooks)
            {
                report.AppendLine($"    {hook}");
            }

            Debug.Log(report.ToString());
        }
    }
}
