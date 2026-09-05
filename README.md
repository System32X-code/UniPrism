# Prism

[![Unity](https://img.shields.io/badge/Unity-2021.3+-brightgreen)](https://unity.com/releases/editor/qa/lts-releases?version=2021.3)

[简体中文](README-CN.md)

Give each Unity editor window its own background image and colour tint.

## Install

Package Manager → **+** → **Add package from git URL**:

```
https://github.com/System32X-code/Prism.git
```

## Use

**Window → Prism**, pick a window, then:

- **Image** — the background for that window. It is encoded into the theme, so the theme stays
  portable even if the source asset moves.
- **Backdrop tint** — multiplies the window's own panels. Lower the alpha and its opaque backdrop
  thins out, letting the image show through.
- **Text and icon tint** — separate, so thinning the backdrop does not wash out the text.
- **Draw over content** — for a window whose backdrop will not thin enough. The image is drawn on
  top instead, watermark style.

A theme lives in your editor preferences folder, so it follows the machine across projects and
Unity versions. **Export** writes it to a `.prism` file to move or share.

**Window → Prism Diagnostics** reports what the painter can actually see. Every failure here is
silent by design — an unhooked host, a title that does not match, an image that did not decode —
so run this before assuming something is broken.

## How it works, and what it cannot do

Editor styles cannot be repainted by editing them. Editor code resolves its styles once, in static
constructors, into static fields, and about two thirds of those are `new GUIStyle(...)` copies
disconnected from the skin. Even patching those live instances directly — including the exact
objects Unity's IMGUI debugger names as the ones used to draw — changes nothing: the values are
written, they survive the whole repaint, and the editor still renders the original. On current
Unity versions the managed `GUIStyle` background and colour fields no longer drive rendering.

What does work is tinting. IMGUI multiplies style backdrops by `GUI.backgroundColor` and text and
icons by `GUI.contentColor` as it draws, rather than reading them back from the style. Prism sets
those around the window's own OnGUI, which is also the one point that sits after the host has
painted its opaque chrome and before the window paints its content — and past `ResetGUIState`,
which clears exactly these values at the top of the host's OnGUI and defeats anything set earlier.

So the granularity is **per window, not per style**. Prism cannot recolour one label and leave the
button next to it alone. That is a limit of what Unity still honours, not an unfinished feature.

Prism reaches three things Unity does not make public: enumerating hosts, the window a host shows,
and the delegate it invokes to draw it. All three are isolated in
[HostViewBridge](Editor/Painting/HostViewBridge.cs) and fail soft — if a future Unity renames one,
Prism goes inert and says so in the diagnostics rather than throwing on every repaint.

## Credits

Prism grew out of debugging [piti6/UniSkin](https://github.com/piti6/UniSkin), which does not work
on Unity 2021.2+ for the reasons above. The mechanism here is different, but the investigation
started there.

MIT licensed — see [LICENSE](LICENSE).
