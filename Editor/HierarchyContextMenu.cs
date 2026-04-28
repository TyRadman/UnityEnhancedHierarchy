using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HierarchyStyler
{
    /// <summary>
    /// Adds "Add Style" and "Clear Style" items to the GameObject context menu in the Hierarchy.
    /// </summary>
    public static class HierarchyContextMenu
    {
        [MenuItem("GameObject/Add Style.../Pick Style...", false, 0)]
        private static void AddStyle(MenuCommand command)
        {
            // Unity calls this once per selected object. Guard so we only show one popup.
            if (Selection.gameObjects.Length > 0 &&
                command.context != Selection.gameObjects[0])
                return;

            var targets = new List<GameObject>(Selection.gameObjects);
            if (targets.Count == 0 && command.context is GameObject go)
                targets.Add(go);

            if (targets.Count == 0) return;

            StylePickerWindow.Show(targets);
        }

        [MenuItem("GameObject/Add Style.../Clear Style", false, 1)]
        private static void ClearStyle(MenuCommand command)
        {
            if (Selection.gameObjects.Length > 0 &&
                command.context != Selection.gameObjects[0])
                return;

            var targets = new List<GameObject>(Selection.gameObjects);
            if (targets.Count == 0 && command.context is GameObject go)
                targets.Add(go);

            foreach (var t in targets)
            {
                var db = HierarchyStyleDatabase.GetForScene(t.scene, createIfMissing: false);
                if (db == null) continue;
                string gid = GlobalObjectId.GetGlobalObjectIdSlow(t).ToString();
                db.RemoveStyle(gid);
            }
            AssetDatabase.SaveAssets();
            EditorApplication.RepaintHierarchyWindow();
        }

        // Validators: only available for scene GameObjects.
        [MenuItem("GameObject/Add Style.../Pick Style...", true)]
        private static bool ValidateAddStyle()
        {
            return Selection.activeGameObject != null &&
                   Selection.activeGameObject.scene.IsValid();
        }

        [MenuItem("GameObject/Add Style.../Clear Style", true)]
        private static bool ValidateClearStyle()
        {
            return Selection.activeGameObject != null &&
                   Selection.activeGameObject.scene.IsValid();
        }
    }

    /// <summary>
    /// Small popup that lists every style and lets the user pick one for the selected GameObjects.
    /// </summary>
    public class StylePickerWindow : EditorWindow
    {
        private List<GameObject> _targets;
        private Vector2 _scroll;

        public static void Show(List<GameObject> targets)
        {
            var win = CreateInstance<StylePickerWindow>();
            win._targets = targets;
            win.titleContent = new GUIContent("Pick a Style");
            win.minSize = new Vector2(260, 200);
            win.maxSize = new Vector2(400, 600);

            // Position near the mouse.
            var mouse = GUIUtility.GUIToScreenPoint(Event.current != null
                ? Event.current.mousePosition
                : Vector2.zero);
            var rect = new Rect(mouse.x, mouse.y, 280, 320);
            win.position = rect;
            win.ShowUtility();
            win.Focus();
        }

        private void OnGUI()
        {
            var library = HierarchyStyleLibrary.Instance;

            EditorGUILayout.LabelField($"Apply style to {_targets?.Count ?? 0} object(s):",
                EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (library.styles.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No styles defined yet.\nOpen Tools > Hierarchy to create one.",
                    MessageType.Info);
                if (GUILayout.Button("Open Hierarchy Styler"))
                {
                    HierarchyStylerWindow.Open();
                    Close();
                }
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var style in library.styles)
            {
                if (style == null) continue;

                var rowRect = EditorGUILayout.BeginHorizontal(GUILayout.Height(22));
                EditorGUI.DrawRect(rowRect, style.backgroundColor);

                if (style.icon != null)
                {
                    GUILayout.Label(style.icon, GUILayout.Width(20), GUILayout.Height(20));
                }
                else
                {
                    GUILayout.Space(20);
                }

                var label = new GUIStyle(EditorStyles.label);
                if (style.bold) label.fontStyle = FontStyle.Bold;
                if (style.textColor.a > 0f) label.normal.textColor = style.textColor;
                GUILayout.Label(style.displayName, label, GUILayout.ExpandWidth(true));

                if (GUILayout.Button("Apply", GUILayout.Width(60)))
                {
                    ApplyStyle(style);
                    Close();
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(2);
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            if (GUILayout.Button("Cancel")) Close();
        }

        private void ApplyStyle(HierarchyStyle style)
        {
            if (_targets == null) return;

            // Group by scene so we only fetch each db once.
            var byScene = new Dictionary<UnityEngine.SceneManagement.Scene, List<GameObject>>();
            foreach (var go in _targets)
            {
                if (go == null) continue;
                if (!byScene.TryGetValue(go.scene, out var list))
                    byScene[go.scene] = list = new List<GameObject>();
                list.Add(go);
            }

            foreach (var kvp in byScene)
            {
                var db = HierarchyStyleDatabase.GetForScene(kvp.Key, createIfMissing: true);
                if (db == null) continue;
                foreach (var go in kvp.Value)
                {
                    string gid = GlobalObjectId.GetGlobalObjectIdSlow(go).ToString();
                    db.SetStyle(gid, style.id);
                }
            }
            AssetDatabase.SaveAssets();
            EditorApplication.RepaintHierarchyWindow();
        }
    }
}
