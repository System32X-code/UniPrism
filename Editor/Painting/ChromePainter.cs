using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniPrism
{
    /// <summary>
    /// Colours the frame around a window - the tab strip and borders the host draws, rather than
    /// the window's own content.
    /// </summary>
    /// <remarks>
    /// This needs a second, outer hook. The host paints its chrome and only then invokes the
    /// window, so <see cref="WindowPainter"/> - which wraps that invocation - runs too late to
    /// tint it, and the GUI inside a window is clipped to the window's own rect anyway.
    /// <para/>
    /// The chrome is washed over after the fact instead of tinted through
    /// <c>GUI.backgroundColor</c> like the content is. There is no seam to tint from:
    /// <c>ResetGUIState</c> runs as the first statement of the host's OnGUI and clears the GUI
    /// colours, and the chrome is drawn immediately after, with nothing in between to hook. A wash
    /// tints the tab labels along with the strip, which is the trade for reaching it at all.
    /// </remarks>
    internal static class ChromePainter
    {
        private static readonly Dictionary<int, Hook> _hooks = new Dictionary<int, Hook>();
        private static readonly List<int> _staleHookIds = new List<int>();

        public static int HookCount => _hooks.Count;

        /// <summary>
        /// One line per hooked host, reporting the inset the wash is confined to. A left inset of
        /// zero means the strip beside the window is not the host's to paint.
        /// </summary>
        public static IEnumerable<string> DescribeHooks()
        {
            foreach (var hook in _hooks.Values)
            {
                yield return hook.Describe();
            }
        }

        public static void Refresh()
        {
            if (!HostViewBridge.IsAvailable) return;

            PruneDeadHooks();

            foreach (var hostView in HostViewBridge.GetHostViews())
            {
                Attach(hostView);
            }
        }

        public static void DetachAll()
        {
            foreach (var hook in _hooks.Values)
            {
                hook.Detach();
            }

            _hooks.Clear();
        }

        private static void PruneDeadHooks()
        {
            foreach (var pair in _hooks)
            {
                if (pair.Value.IsAlive) continue;

                _staleHookIds.Add(pair.Key);
            }

            foreach (var staleHookId in _staleHookIds)
            {
                _hooks.Remove(staleHookId);
            }

            _staleHookIds.Clear();
        }

        private static void Attach(ScriptableObject hostView)
        {
            if (hostView == null) return;

            var container = HostViewBridge.GetChromeContainer(hostView);
            if (container == null) return;

            var hostViewId = hostView.GetInstanceID();

            if (_hooks.TryGetValue(hostViewId, out var existing))
            {
                //A rebuilt view gets a new container, and the old hook still looks healthy on it.
                if (existing.IsInstalledOn(container)) return;

                _hooks.Remove(hostViewId);
            }

            var hook = Hook.Create(hostView, container);
            if (hook == null) return;

            _hooks[hostViewId] = hook;
        }

        private sealed class Hook
        {
            private readonly ScriptableObject _hostView;
            private readonly IMGUIContainer _container;
            private readonly Action _original;
            private readonly Action _installed;

            private Hook(ScriptableObject hostView, IMGUIContainer container, Action original)
            {
                _hostView = hostView;
                _container = container;
                _original = original;
                _installed = OnGUI;
            }

            public static Hook Create(ScriptableObject hostView, IMGUIContainer container)
            {
                var original = container.onGUIHandler;
                if (original == null) return null;

                var hook = new Hook(hostView, container, original);
                container.onGUIHandler = hook._installed;

                return hook;
            }

            public bool IsAlive => _hostView != null && _container.onGUIHandler == _installed;

            public bool IsInstalledOn(IMGUIContainer container)
            {
                return ReferenceEquals(container, _container) && IsAlive;
            }

            public void Detach()
            {
                //Leave a hook someone else layered on top of us alone.
                if (_container.onGUIHandler != _installed) return;

                _container.onGUIHandler = _original;
            }

            public string Describe()
            {
                var window = HostViewBridge.GetActualView(_hostView);
                var title = window == null ? "<none>" : window.titleContent.text;
                var border = HostViewBridge.GetBorderSize(_hostView);
                var insets = border == null
                    ? "borderSize unavailable"
                    : $"insets L{border.left} R{border.right} T{border.top} B{border.bottom}";

                return $"{title} - container {_container.contentRect.width:F0}x{_container.contentRect.height:F0}, {insets}";
            }

            private void OnGUI()
            {
                _original.Invoke();

                if (Event.current == null || Event.current.type != EventType.Repaint) return;

                var window = HostViewBridge.GetActualView(_hostView);
                if (window == null) return;

                var title = window.titleContent?.text;
                if (string.IsNullOrEmpty(title)) return;

                var wash = EffectiveAppearance.Resolve(ThemeStore.Theme, title).ChromeTint;
                if (wash.a <= 0f) return;

                foreach (var band in ChromeBands(window))
                {
                    if (band.width <= 0f || band.height <= 0f) continue;

                    EditorGUI.DrawRect(band, wash);
                }
            }

            /// <summary>
            /// Everything the host keeps for itself: the tab strip above the window's content and
            /// the borders down either side and below it.
            /// </summary>
            /// <remarks>
            /// Four bands around the content rather than one rect over everything, because the
            /// content is painted by the window and washing over it would undo the tint that was
            /// carefully applied there.
            /// </remarks>
            private IEnumerable<Rect> ChromeBands(EditorWindow window)
            {
                var container = _container.contentRect;
                if (container.width <= 0f || container.height <= 0f) yield break;

                var content = ContentRect(container, window);

                //Top: the tab strip, and the tallest of the four by far.
                yield return new Rect(0f, 0f, container.width, content.y);
                yield return new Rect(0f, content.yMax, container.width, container.height - content.yMax);
                yield return new Rect(0f, content.y, content.x, content.height);
                yield return new Rect(content.xMax, content.y, container.width - content.xMax, content.height);
            }

            private Rect ContentRect(Rect container, EditorWindow window)
            {
                var border = HostViewBridge.GetBorderSize(_hostView);

                if (border != null)
                {
                    return new Rect(
                        border.left,
                        border.top,
                        Mathf.Max(0f, container.width - border.horizontal),
                        Mathf.Max(0f, container.height - border.vertical));
                }

                //Without the border insets, fall back to what the height difference reveals: the
                //tab strip, and nothing down the sides.
                var chromeHeight = Mathf.Clamp(container.height - window.position.height, 0f, container.height);

                return new Rect(0f, chromeHeight, container.width, container.height - chromeHeight);
            }
        }
    }
}
