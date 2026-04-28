using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HierarchyStyler
{
    /// <summary>
    /// Hooks into the Hierarchy window and draws the configured style for each entry.
    /// </summary>
    [InitializeOnLoad]
    public static class HierarchyStyleDrawer
    {
        // Width of the icon area on the right side of the row.
        private const float IconSize = 16f;
        private const float IconPadding = 2f;

        // Tint amounts applied to the background for interaction states.
        private const float HoverLighten = 0.12f;
        private const float SelectionLighten = 0.25f;
        // Selection also gets a subtle blue shift so it's clearly distinguishable
        // from hover even on already-blue styles.
        private static readonly Color SelectionTint = new Color(0.24f, 0.49f, 0.91f); // Unity's selection blue

        static HierarchyStyleDrawer()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;

            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;

            EditorSceneManager.sceneClosed -= OnSceneClosed;
            EditorSceneManager.sceneClosed += OnSceneClosed;

            // Poll for mouse movement so hover repaints fire even though
            // hierarchyWindowItemOnGUI doesn't receive MouseMove events.
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            // Only force a hierarchy repaint when the mouse actually moved.
            // The hover hit-test itself happens during the resulting Repaint pass.
            var hierarchyHovered = EditorWindow.mouseOverWindow;
            if (hierarchyHovered == null) return;
            if (hierarchyHovered.GetType().Name != "SceneHierarchyWindow") return;

            // Use the window-relative mouse via a static field cached during Repaint;
            // simpler: just always repaint while the cursor is over the hierarchy.
            // Cheap because Unity coalesces repaints.
            EditorApplication.RepaintHierarchyWindow();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            // Force fresh load so the cache picks up renamed/moved scene assets.
            HierarchyStyleDatabase.InvalidateCache();
            EditorApplication.RepaintHierarchyWindow();
        }

        private static void OnSceneClosed(Scene scene)
        {
            HierarchyStyleDatabase.InvalidateCache();
        }

        private static void OnHierarchyGUI(int instanceId, Rect selectionRect)
        {
            var go = EditorUtility.InstanceIDToObject(instanceId) as GameObject;
            if (go == null) return;

            var db = HierarchyStyleDatabase.GetForScene(go.scene, createIfMissing: false);
            if (db == null || db.entries.Count == 0) return;

            string gid = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString();
            string styleId = db.GetStyleIdFor(gid);
            if (string.IsNullOrEmpty(styleId)) return;

            var style = HierarchyStyleLibrary.Instance.GetById(styleId);
            if (style == null)
            {
                // Self-heal: style was deleted from the library.
                db.RemoveStyle(gid);
                return;
            }

            // Hover detection: with continuous hierarchy repaints (driven from
            // OnEditorUpdate while the cursor is over the window), checking
            // mouse-in-rect here gives correct per-row hover state.
            Rect fullRow = selectionRect;
            fullRow.x -= 16f;
            fullRow.width += 16f;

            bool isSelected = Selection.Contains(go);
            bool isHovered = !isSelected &&
                             Event.current.type == EventType.Repaint &&
                             fullRow.Contains(Event.current.mousePosition);

            DrawStyle(go, selectionRect, style, isSelected, isHovered);
        }

        private static void DrawStyle(GameObject go, Rect rect, HierarchyStyle style,
            bool isSelected, bool isHovered)
        {
            // Expand the rect leftward to cover the foldout arrow area for parents.
            // Unity's foldout arrow sits roughly 14px to the left of the label rect.
            Rect bgRect = rect;
            bgRect.x -= 16f;
            bgRect.width += 16f;

            // Compute the background color, applying interaction-state tints.
            Color bg = style.backgroundColor;
            if (isSelected)
            {
                // Blend toward Unity's selection blue, then lighten so the style
                // identity still reads through.
                bg = Color.Lerp(bg, SelectionTint, 0.55f);
                bg = Lighten(bg, SelectionLighten);
                // Ensure selection always reads as opaque.
                bg.a = Mathf.Max(bg.a, 0.85f);
            }
            else if (isHovered)
            {
                bg = Lighten(bg, HoverLighten);
                bg.a = Mathf.Max(bg.a, Mathf.Min(1f, style.backgroundColor.a + 0.15f));
            }

            // 1. Paint background (this overshadows the arrow).
            if (bg.a > 0f)
            {
                EditorGUI.DrawRect(bgRect, bg);
            }

            // 2. Re-draw the GameObject icon + label on top so they stay visible.
            //    We pull the icon from EditorGUIUtility so prefabs/disabled objects look right.
            var content = EditorGUIUtility.ObjectContent(go, typeof(GameObject));
            var labelRect = rect;

            // Draw the icon (the small one to the left of the name).
            if (content.image != null)
            {
                var iconRect = new Rect(rect.x, rect.y, 16f, 16f);
                GUI.DrawTexture(iconRect, content.image, ScaleMode.ScaleToFit);
            }

            // Draw the name text.
            labelRect.x += 18f;
            labelRect.width -= 18f;

            var labelStyle = new GUIStyle(EditorStyles.label);
            if (style.bold) labelStyle.fontStyle = FontStyle.Bold;

            Color textColor = style.textColor.a > 0f
                ? style.textColor
                : (EditorGUIUtility.isProSkin ? Color.white : Color.black);

            // When selected, force readable white text on the blue-tinted background.
            if (isSelected)
                textColor = Color.white;

            // Dim text for inactive GameObjects, matching Unity's default look.
            if (!go.activeInHierarchy)
                textColor.a *= 0.5f;

            labelStyle.normal.textColor = textColor;
            labelStyle.onNormal.textColor = textColor;

            GUI.Label(labelRect, go.name, labelStyle);

            // 3. Draw the side icon on the right.
            if (style.icon != null)
            {
                var iconRect = new Rect(
                    rect.xMax - IconSize - IconPadding,
                    rect.y + (rect.height - IconSize) * 0.5f,
                    IconSize,
                    IconSize);
                GUI.DrawTexture(iconRect, style.icon, ScaleMode.ScaleToFit);
            }
        }

        /// <summary>
        /// Lightens a color toward white by the given amount [0..1], preserving alpha.
        /// </summary>
        private static Color Lighten(Color c, float amount)
        {
            return new Color(
                Mathf.Lerp(c.r, 1f, amount),
                Mathf.Lerp(c.g, 1f, amount),
                Mathf.Lerp(c.b, 1f, amount),
                c.a);
        }
    }
}
