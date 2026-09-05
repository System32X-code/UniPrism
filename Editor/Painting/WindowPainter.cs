using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UniPrism
{
    /// <summary>
    /// Applies each window's appearance by wrapping the delegate its host invokes to draw it.
    /// </summary>
    /// <remarks>
    /// Two things make this work, and both were arrived at the hard way.
    /// <para/>
    /// <b>Where.</b> A host draws its own opaque chrome first and only then invokes the window, so
    /// anything painted from further out is covered up. The delegate the host holds for the
    /// window's OnGUI is the one point that sits after the chrome and before the content.
    /// <para/>
    /// <b>How.</b> Editor styles cannot be repainted by editing them. Editor code snapshots its
    /// styles into static fields in static constructors, and current Unity versions no longer read
    /// the managed background and colour fields when rendering at all - they can be written, they
    /// survive the whole repaint on the object the IMGUI debugger names as the one used, and the
    /// editor still draws the original. What does work is tinting: IMGUI multiplies style
    /// backdrops by <c>GUI.backgroundColor</c> and text and icons by <c>GUI.contentColor</c> as it
    /// draws, so thinning a window's backdrop to reveal an image behind it leaves its text
    /// legible. That slot is also past <c>ResetGUIState</c>, which clears exactly these values at
    /// the top of the host's OnGUI and defeats anything set earlier.
    /// </remarks>
    internal static class WindowPainter
    {
        private static readonly Dictionary<int, Hook> _hooks = new Dictionary<int, Hook>();
        private static readonly List<int> _staleHookIds = new List<int>();

        //Rate limit for the repaint broadcast below, in editor seconds.
        private const double BroadcastInterval = 1d / 60d;

        private static double _lastBroadcastTime;

        public static int HookCount => _hooks.Count;

        /// <summary>
        /// Attaches to hosts that are not hooked yet and drops hooks whose host is gone. Cheap
        /// enough to poll, which is how newly opened windows get picked up.
        /// </summary>
        public static void Refresh()
        {
            if (!HostViewBridge.IsAvailable) return;

            PruneDeadHooks();

            foreach (var hostView in HostViewBridge.GetHostViews())
            {
                Attach(hostView);
            }
        }

        /// <summary>
        /// Puts every host back the way it was. Called before a domain reload so no host is left
        /// holding a delegate from a domain that no longer exists.
        /// </summary>
        public static void DetachAll()
        {
            foreach (var hook in _hooks.Values)
            {
                hook.Detach();
            }

            _hooks.Clear();
        }


        /// <summary>
        /// Asks every other themed window to repaint, because one of them moved.
        /// </summary>
        /// <remarks>
        /// A window spanning the shared image shows the slice that falls behind it, so its slice
        /// changes when *any* window moves or resizes - not just when it does. Unity repaints the
        /// windows directly involved in a drag, which is why the others were left showing a stale
        /// slice until the drag ended and something else forced them to redraw.
        /// <para/>
        /// The broadcast comes from a window that has noticed its own rect change, so it costs
        /// nothing while the layout is still, and it is rate limited because during a drag every
        /// window notices the change in the same frame.
        /// </remarks>
        private static void BroadcastLayoutChange(ScriptableObject origin)
        {
            var now = EditorApplication.timeSinceStartup;
            if (now - _lastBroadcastTime < BroadcastInterval) return;

            _lastBroadcastTime = now;

            foreach (var hook in _hooks.Values)
            {
                hook.RepaintUnless(origin);
            }
        }

        public static IEnumerable<string> DescribeHooks()
        {
            foreach (var hook in _hooks.Values)
            {
                yield return hook.Describe();
            }
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

            var current = HostViewBridge.GetOnGUI(hostView);

            //Null until the host has a window to show.
            if (current == null) return;

            var hostViewId = hostView.GetInstanceID();

            if (_hooks.TryGetValue(hostViewId, out var existing))
            {
                // Comparing the installed delegate, not just remembering that we hooked: the host
                // rebuilds it whenever the window it shows changes, which silently drops us.
                if (existing.IsInstalled(current)) return;

                _hooks.Remove(hostViewId);
            }

            var hook = Hook.Create(hostView, current);
            if (hook == null) return;

            _hooks[hostViewId] = hook;
        }

        private sealed class Hook
        {
            private readonly ScriptableObject _hostView;
            private readonly Delegate _original;
            private readonly Action _originalInvoke;

            private Delegate _installed;
            private int _drawCount;
            private int _paintCount;
            private Rect _lastScreenRect;

            private Hook(ScriptableObject hostView, Delegate original, Action originalInvoke)
            {
                _hostView = hostView;
                _original = original;
                _originalInvoke = originalInvoke;
            }

            public static Hook Create(ScriptableObject hostView, Delegate original)
            {
                var originalInvoke = AsAction(original);
                if (originalInvoke == null) return null;

                var delegateType = HostViewBridge.OnGUIDelegateType;
                if (delegateType == null) return null;

                var hook = new Hook(hostView, original, originalInvoke);

                try
                {
                    //Built against the field's own delegate type, which is protected and unnameable.
                    hook._installed = Delegate.CreateDelegate(delegateType, hook, nameof(Invoke));
                }
                catch (Exception)
                {
                    return null;
                }

                return HostViewBridge.SetOnGUI(hostView, hook._installed) ? hook : null;
            }

            public bool IsAlive => _hostView != null;

            public bool IsInstalled(Delegate current) => ReferenceEquals(current, _installed);

            public void Detach()
            {
                if (_hostView == null) return;

                // Only unwind our own wrapper. If something else hooked on top, replacing the
                // delegate would silently drop that other hook.
                if (!ReferenceEquals(HostViewBridge.GetOnGUI(_hostView), _installed)) return;

                HostViewBridge.SetOnGUI(_hostView, _original);
            }

            public string Describe()
            {
                var window = HostViewBridge.GetActualView(_hostView);
                var title = window == null ? "<none>" : window.titleContent.text;

                return $"{title} - draws: {_drawCount}, painted: {_paintCount}";
            }

            public void Invoke()
            {
                _drawCount++;

                var window = HostViewBridge.GetActualView(_hostView);
                if (window == null)
                {
                    _originalInvoke.Invoke();
                    return;
                }

                var appearance = EffectiveAppearance.Resolve(ThemeStore.Theme, window.titleContent?.text);
                if (appearance.IsNeutral)
                {
                    _originalInvoke.Invoke();
                    return;
                }

                var rect = new Rect(0f, 0f, window.position.width, window.position.height);
                var isRepaint = Event.current != null && Event.current.type == EventType.Repaint;
                var overContent = appearance.HasBackground && appearance.Background.DrawOverContent;

                if (isRepaint && appearance.HasBackground && appearance.Background.SpanEditor)
                {
                    NoticeLayoutChange(rect);
                }

                if (isRepaint && !overContent && PaintBackground(appearance, rect))
                {
                    _paintCount++;
                }

                InvokeTinted(appearance);

                if (isRepaint && overContent && PaintBackground(appearance, rect))
                {
                    _paintCount++;
                }
            }


            private void InvokeTinted(EffectiveAppearance appearance)
            {
                var backdropTint = appearance.BackdropTint;
                var contentTint = appearance.ContentTint;

                if (backdropTint == Color.white && contentTint == Color.white)
                {
                    _originalInvoke.Invoke();
                    return;
                }

                // Text is tinted through the styles, which icons do not read, so they keep their
                // own colours. Including icons means falling back to the shared multiplier.
                var viaStyles = !appearance.TintIcons && contentTint != Color.white;

                var previousBackground = GUI.backgroundColor;
                var previousContent = GUI.contentColor;

                GUI.backgroundColor = previousBackground * backdropTint;

                if (!viaStyles)
                {
                    GUI.contentColor = previousContent * contentTint;
                }

                if (viaStyles)
                {
                    TextTint.Apply(contentTint);
                }

                try
                {
                    _originalInvoke.Invoke();
                }
                finally
                {
                    //Shared styles: they have to be back before any other view draws.
                    TextTint.Restore();

                    GUI.backgroundColor = previousBackground;
                    GUI.contentColor = previousContent;
                }
            }

            /// <summary>
            /// Tells the others when this window has moved, so their slice of the shared image
            /// keeps up during a drag rather than catching up when it ends.
            /// </summary>
            private void NoticeLayoutChange(Rect rect)
            {
                var topLeft = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
                var screenRect = new Rect(topLeft.x, topLeft.y, rect.width, rect.height);

                //Sub-pixel jitter is not a layout change; redrawing every window for it would be.
                if (Approximately(screenRect, _lastScreenRect)) return;

                _lastScreenRect = screenRect;

                BroadcastLayoutChange(_hostView);
            }

            private static bool Approximately(Rect a, Rect b)
            {
                return Mathf.Abs(a.x - b.x) < 0.5f
                    && Mathf.Abs(a.y - b.y) < 0.5f
                    && Mathf.Abs(a.width - b.width) < 0.5f
                    && Mathf.Abs(a.height - b.height) < 0.5f;
            }

            public void RepaintUnless(ScriptableObject origin)
            {
                if (ReferenceEquals(_hostView, origin)) return;

                HostViewBridge.GetActualView(_hostView)?.Repaint();
            }

            private static bool PaintBackground(EffectiveAppearance appearance, Rect rect)
            {
                if (!appearance.HasBackground) return false;

                var settings = appearance.Background;
                var texture = ThemeStore.Theme.FindTexture(settings.BackgroundTextureId)?.Texture;
                if (texture == null) return false;

                var previousColor = GUI.color;
                GUI.color = previousColor * settings.BackgroundTint;

                try
                {
                    ImageFraming.Draw(rect, texture, settings, spanEditor: true);
                }
                finally
                {
                    GUI.color = previousColor;
                }

                return true;
            }

            private static Action AsAction(Delegate source)
            {
                try
                {
                    //Rebound as a plain Action so repaints do not pay for DynamicInvoke.
                    return (Action)Delegate.CreateDelegate(typeof(Action), source.Target, source.Method);
                }
                catch (Exception)
                {
                    return () => source.DynamicInvoke();
                }
            }
        }
    }
}
