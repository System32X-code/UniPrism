<div align="center">

# UniPrism

**Give each Unity editor window its own background image and colour scheme**

[![Unity](https://img.shields.io/badge/Unity-2022.3%2B-brightgreen)](https://unity.com/releases/editor/qa/lts-releases?version=2022.3)
[![License](https://img.shields.io/badge/license-MIT-blue)](LICENSE)
[![Language](https://img.shields.io/badge/UI-%E4%B8%AD%20%2F%20EN%20%2F%20%E6%97%A5-lightgrey)](#interface-language)

[简体中文](README.md) · English · [日本語](README-JA.md)

</div>

---

## What it is

UniPrism changes how the Unity editor looks: a wallpaper across the whole thing, a different colour for each window, text in whatever colour you like. Everything lives in a theme that follows your machine across projects and Unity versions, and exports to a single file you can share.

It needs **no dependencies** and does not touch your Unity installation.

## Features

**Background**

- A background image per window, or **one image across the whole editor** — each window shows the part behind it, so they line up into a single continuous picture instead of every window scaling its own copy
- Framing controls: crop, fit or stretch, with zoom and alignment, and a **live preview**
- Pick images straight off disk: no importing into the project, and none of the importer's compression or size cap

**Colour**

- A three-colour palette. Windows point at a slot rather than copying the colour, so editing the palette recolours everything using it at once
- Three independently tinted regions: the **window backdrop**, its **text** (optionally including icons), and the **window frame** — the dock's tab strip
- Tinting text leaves icons alone, by default

**Organisation**

- Global settings cover every window; any single window can override the background, the colours, or both
- Themes live in the editor preferences folder, so they apply across projects, and export to a `.prism` file with images inlined
- Interface in 中文 / English / 日本語

## Install

Package Manager → **+** → **Add package from git URL**:

```
https://github.com/System32X-code/UniPrism.git
```

Or add it to `dependencies` in `Packages/manifest.json`:

```json
"com.system32x.uniprism": "https://github.com/System32X-code/UniPrism.git"
```

## Usage

Open **Window → UniPrism**. There are three pages.

### Global

The baseline every window starts from.

| Setting | What it does |
|---|---|
| Palette | Primary / Secondary / Tertiary. Anything below set to a palette slot follows it |
| Image | The background. **Browse...** picks a file from disk, **Framing...** adjusts the crop |
| One image across the editor | All windows share a single continuous picture |
| Window backdrop | Lower the opacity and the window's own backdrop thins out, letting the image through |
| Text | Tinted separately, leaving icons alone unless you tick *Tint icons too* |
| Window frame | The dock's tab strip and borders |

### Per window

Pick a window and decide whether it departs from the global look:

- **Override the global background**
- **Override the global colours**

With both off it simply follows the global settings. For deliberate contrast, put one group of windows on Primary and another on Tertiary.

### About

Author and project links, plus **Developer → Log diagnostics report**, which writes what the painters can actually see to the console. **Run this before assuming something is broken** — it reports whether a host is hooked, whether a window title matches, and whether an image decoded.

## How it works

UniPrism wraps the delegate a host view invokes to draw its window. That is the one usable seam: the host has already painted its opaque chrome, the window has not yet painted its content, and it is past `ResetGUIState` — which clears the GUI colours as the first statement of the host's OnGUI and discards anything set further out.

Colour is applied by tinting at draw time rather than by editing styles:

| Channel | Reaches |
|---|---|
| `GUI.backgroundColor` | Style backdrops. Lowering the alpha thins them so the image shows through |
| `GUIStyle.textColor` | Text. Icons do not read this field, which is what allows text to be tinted alone |
| `GUI.contentColor` | Text and icons together — used only when *Tint icons too* is on |

One finding worth recording: on current Unity versions a style's `background` field **no longer drives rendering**. It can be written, it survives the entire repaint, on the very object Unity's IMGUI debugger names as the one used to draw — and the editor still renders the original. `textColor` is still read. The two are not interchangeable.

The three things Unity does not make public — enumerating host views, the window a host shows, and the delegate it draws through — are confined to [`HostViewBridge`](Editor/Painting/HostViewBridge.cs) and fail soft. If a future Unity renames one, UniPrism goes inert and says which member it could not resolve, rather than throwing on every repaint.

## Known limitations

- **Per window, not per control.** UniPrism cannot recolour one label and leave the button beside it alone. That is a limit of what Unity still honours, not an unfinished feature.
- **The frame is washed over, not tinted**, so tab labels take the colour too. The tab strip is drawn by the dock with no seam in between to tint from. A strength of 0.2–0.4 usually reads best.
- **The outermost gutter of the main window cannot be reached.** `SplitView` and `MainView` draw no pixels at all; that strip is the container window's own background, with no IMGUI code painting it.
- **Dragging a floating window by its title bar does not update the background live.** The OS is moving a bitmap at that point and Unity is not drawing. Dragging splitters and resizing docked windows are live.

## Compatibility

- Unity **2022.3+**
- Verified on **2022.3.42f1c1**

UniPrism uses internal Unity API. The dependency is as small as it can be — three members, in one file — but a major Unity release could still break it, in which case it stops quietly and says why.

## Interface language

The dropdown at the right of the toolbar switches between **中文 / English / 日本語**. The choice is stored in `EditorPrefs` and defaults to your system language.

## Contributing

Issues and pull requests are welcome.

The Japanese and Chinese text was written by a non-native speaker; corrections are appreciated.

## Acknowledgements

UniPrism grew out of debugging [piti6/UniSkin](https://github.com/piti6/UniSkin), which stopped working on Unity 2021.2+. The mechanism here is entirely different and the code is a rewrite, but the initial direction and several key findings came out of that investigation.

## License

[MIT](LICENSE)
