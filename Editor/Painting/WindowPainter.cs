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
        /// Draws an image the way the painter would, for the settings preview. Sharing the routine
        /// is the point: a preview that approximates the real framing is worse than none.
        /// </summary>
        public static void DrawFramedPreview(Rect rect, Texture2D texture, WindowAppearance appearance)
        {
            Hook.DrawFramed(rect, texture, appearance);
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
                var appearance = FindAppearance(window);

                if (appearance == null)
                {
                    _originalInvoke.Invoke();
                    return;
                }

                var rect = new Rect(0f, 0f, window.position.width, window.position.height);
                var isRepaint = Event.current != null && Event.current.type == EventType.Repaint;

                if (isRepaint && !appearance.DrawOverContent && PaintBackground(appearance, rect))
                {
                    _paintCount++;
                }

                InvokeTinted(appearance);

                if (isRepaint && appearance.DrawOverContent && PaintBackground(appearance, rect))
                {
                    _paintCount++;
                }
            }

            private static WindowAppearance FindAppearance(EditorWindow window)
            {
                if (window == null) return null;

                var title = window.titleContent?.text;

                return string.IsNullOrEmpty(title) ? null : ThemeStore.Theme.Find(title);
            }

            private void InvokeTinted(WindowAppearance appearance)
            {
                var backdropTint = appearance.ResolveBackdrop(ThemeStore.Theme.Palette);
                var contentTint = appearance.ContentTint;

                if (backdropTint == Color.white && contentTint == Color.white)
                {
                    _originalInvoke.Invoke();
                    return;
                }

                var previousBackground = GUI.backgroundColor;
                var previousContent = GUI.contentColor;

                GUI.backgroundColor = previousBackground * backdropTint;
                GUI.contentColor = previousContent * contentTint;

                try
                {
                    _originalInvoke.Invoke();
                }
                finally
                {
                    GUI.backgroundColor = previousBackground;
                    GUI.contentColor = previousContent;
                }
            }

            private static bool PaintBackground(WindowAppearance appearance, Rect rect)
            {
                if (!appearance.HasBackground) return false;

                var texture = ThemeStore.Theme.FindTexture(appearance.BackgroundTextureId)?.Texture;
                if (texture == null) return false;

                var previousColor = GUI.color;
                GUI.color = previousColor * appearance.BackgroundTint;

                try
                {
                    DrawFramed(rect, texture, appearance);
                }
                finally
                {
                    GUI.color = previousColor;
                }

                return true;
            }

            /// <summary>
            /// Crop is drawn through texture coordinates rather than <c>ScaleMode.ScaleAndCrop</c>,
            /// which always centres the image and offers no zoom.
            /// </summary>
            internal static void DrawFramed(Rect rect, Texture2D texture, WindowAppearance appearance)
            {
                switch (appearance.ImageScaleMode)
                {
                    case ImageScaleMode.Stretch:
                        GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill);
                        return;

                    case ImageScaleMode.Fit:
                        GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
                        return;
                }

                if (rect.height <= 0f || texture.height <= 0) return;

                var targetAspect = rect.width / rect.height;
                var imageAspect = (float)texture.width / texture.height;

                //The fraction of the image that covers the window before zoom.
                var width = imageAspect > targetAspect ? targetAspect / imageAspect : 1f;
                var height = imageAspect > targetAspect ? 1f : imageAspect / targetAspect;

                var zoom = appearance.ImageZoom;
                width = Mathf.Clamp01(width / zoom);
                height = Mathf.Clamp01(height / zoom);

                var alignment = appearance.ImageAlignment;

                //Texture coordinates start at the bottom left, alignment reads top-down.
                var x = (1f - width) * alignment.x;
                var y = (1f - height) * (1f - alignment.y);

                GUI.DrawTextureWithTexCoords(rect, texture, new Rect(x, y, width, height));
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
