using UnityEditor;

namespace UniPrism
{
    [InitializeOnLoad]
    internal static class Entrypoint
    {
        /// <summary>
        /// How soon a newly opened window picks up its appearance. Rescanning on every editor
        /// update instead would walk every host a hundred times a second to find nothing.
        /// </summary>
        private const double RefreshIntervalSeconds = 0.25d;

        private static double _nextRefreshTime;

        static Entrypoint()
        {
            EditorApplication.update -= Update;
            EditorApplication.update += Update;

            AssemblyReloadEvents.beforeAssemblyReload -= DetachAll;
            AssemblyReloadEvents.beforeAssemblyReload += DetachAll;

            EditorApplication.quitting -= DetachAll;
            EditorApplication.quitting += DetachAll;
        }

        private static void Update()
        {
            var now = EditorApplication.timeSinceStartup;
            if (now < _nextRefreshTime) return;

            _nextRefreshTime = now + RefreshIntervalSeconds;

            WindowPainter.Refresh();
            ChromePainter.Refresh();
        }

        private static void DetachAll()
        {
            WindowPainter.DetachAll();
            ChromePainter.DetachAll();
        }
    }
}
