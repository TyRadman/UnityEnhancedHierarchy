# Hierarchy Enhancer X

Hierarchy Enhancer X is a lightweight Unity Editor tool that highlights and organizes GameObjects in the Hierarchy window using custom color tags, icons, and alignment based on name prefixes.

## Features

- Custom background colors for GameObjects based on prefixes  
- Optional icons and tooltips  
- Alignment control (left or right)  
- Fade on hover and transparency when selected  
- Fully managed via a dedicated editor window  
- Self-contained as a Unity package — no runtime impact

## Installation

Add this line to your project's `manifest.json` under `dependencies`:

```json
"com.tyradman.hierarchyenhancerx": "https://github.com/tyradman/hierarchyenhancerx.git"
```

Or use the Unity Package Manager:  
**Window → Package Manager → Add package from Git URL**

## 🛠️ Usage

1. After importing, go to  
   **Tools → Hierarchy Highlighter Manager**

2. Use the window to:
   - Add new styles
   - Assign a prefix (e.g. `===`)
   - Choose a color
   - Optionally add an icon and tooltip
   - Choose text alignment (left or right)

3. Rename your GameObjects using the prefix to trigger the style:
   ```
   === EnemySpawner
   /// AudioManager
   ```

## Notes

- Prefixes must be **at least 3 characters** to be valid  
- Duplicate prefixes are not allowed — the manager will warn you  
- The highlighting system is **editor-only** and does not affect builds  
- The style asset is bundled and read-only, keeping user changes scoped to UI only

---


## TODOs:
- Name field for different styles.
- Add highlight and selection colors for objects rather than changing the transparency.
- Fix the issue where compiling code causes the hierarchy manager to fail to load the styles asset.
