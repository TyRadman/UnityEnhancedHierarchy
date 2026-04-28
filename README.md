# Hierarchy Styler

Customize Unity's Hierarchy window with reusable visual styles. Apply background colors, side icons, and text formatting to GameObjects to make complex scenes scannable at a glance.

![Editor only](https://img.shields.io/badge/Unity-Editor%20Only-blue)
![Unity 2021.3+](https://img.shields.io/badge/Unity-2021.3%2B-green)

## Features

- **Reusable styles** — define background color, side icon, bold text, and text color once, apply anywhere.
- **Per-scene database** — each scene's styled entries are stored in a `<SceneName>_HierarchyStyles.asset` next to the scene, so they version-control cleanly and follow the scene if it moves.
- **Right-click to style** — `Add Style... → Pick Style...` on any hierarchy entry opens a small picker.
- **Background covers the foldout arrow** so parent entries get the same clean look as leaf entries.
- **Hover and selection feedback** — styled rows lighten on hover and blend toward Unity's selection blue when selected, while keeping their style identity readable.
- **Self-healing** — entries pointing at deleted GameObjects or removed styles are pruned automatically (or manually via the manager window).
- **Editor-only** — zero runtime cost. No MonoBehaviours, no scene bloat.

## Installation

### Option 1 — Unity Package Manager (Git URL)

1. Open `Window → Package Manager`.
2. Click the **+** dropdown → **Add package from git URL...**
3. Enter:
   ```
   https://github.com/<your-username>/<your-repo>.git
   ```

### Option 2 — Local package

1. Clone or download this repo.
2. In Unity, `Window → Package Manager → + → Add package from disk...`
3. Select the `package.json` at the root of the package.

## Usage

### Create a style

1. `Tools → Hierarchy` opens the **Hierarchy Styler** window.
2. On the **Styles** tab, click **+ New Style**.
3. Set a display name, background color, optional side icon, and text options.

### Apply a style

1. Right-click a GameObject in the Hierarchy.
2. `Add Style... → Pick Style...`
3. Click **Apply** on the style you want.

You can multi-select GameObjects to apply a style to several at once.

### Clear a style

Right-click → `Add Style... → Clear Style`.

### Inspect or clean up entries

The **Scene Entries** tab in the manager window lists every styled entry in currently-loaded scenes, with a per-scene **Prune** button to drop entries whose target GameObject was deleted, plus a global **Prune All Open Scenes**.

## How it works

- Styles live in a single library asset created at `Assets/HierarchyStyler/HierarchyStyleLibrary.asset` on first use.
- Per-scene mappings use `GlobalObjectId` so entries survive instance-id resets, renames, and reparenting.
- A repaint poll (`EditorApplication.update`) drives hover state while the cursor is over the Hierarchy window — and only while it's there.

## License

MIT — see [LICENSE.md](LICENSE.md).
