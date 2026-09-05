using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UniPrism
{
    /// <summary>
    /// Reports what the painters can actually see.
    /// </summary>
    /// <remarks>
    /// Every way this can fail is silent by design - a host that is not hooked, a title that does
    /// not match, an image that did not decode - so without a report there is no way to tell them
    /// apart from "nothing happened".
    /// <para/>
    /// Reached from the About page rather than the Window menu: it is a support tool, not
    /// something to put in front of everyone who opens that menu.
    /// </remarks>
    internal static class Diagnostics
    {
        internal static void LogReport()
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
            report.AppendLine($"Theme '{theme.Name}': {theme.Windows.Count} override(s), {theme.Textures.Count} image(s)");
            report.AppendLine($"    global: {DescribeSettings(theme.Global)}");

            foreach (var appearance in theme.Windows)
            {
                if (appearance == null) continue;

                report.AppendLine($"    [{appearance.WindowTitle}] background override: {appearance.OverridesBackground}"
                    + $", colour override: {appearance.OverridesColours}");

                if (appearance.OverridesBackground || appearance.OverridesColours)
                {
                    report.AppendLine($"        {DescribeSettings(appearance.Settings)}");
                }
            }

            report.AppendLine();

            var hooks = WindowPainter.DescribeHooks().ToArray();
            report.AppendLine($"Hooked hosts: {hooks.Length}");

            foreach (var hook in hooks)
            {
                report.AppendLine($"    {hook}");
            }

            report.AppendLine();
            report.AppendLine($"Chrome hooks: {ChromePainter.HookCount}");

            foreach (var hook in ChromePainter.DescribeHooks())
            {
                report.AppendLine($"    {hook}");
            }

            Debug.Log(report.ToString());
        }

        private static string DescribeSettings(AppearanceSettings settings)
        {
            var theme = ThemeStore.Theme;
            var texture = theme.FindTexture(settings.BackgroundTextureId);

            var image = !settings.HasBackground
                ? "none"
                : texture == null
                    ? "MISSING from theme"
                    : $"{texture.ByteCount} byte(s), {(texture.FromSourceFile ? "source file" : "re-encoded")}"
                        + $", decodes to {(texture.Texture == null ? "<null>" : $"{texture.Texture.width}x{texture.Texture.height}")}";

            return $"image: {image}, span: {settings.SpanEditor}, over content: {settings.DrawOverContent}"
                + $", backdrop: {settings.BackdropSlot}/{settings.BackdropTint}"
                + $", text: {settings.ContentSlot}/{settings.ContentTint}"
                + $", frame: {settings.ChromeSlot}/{settings.ChromeTint}";
        }
    }
}
