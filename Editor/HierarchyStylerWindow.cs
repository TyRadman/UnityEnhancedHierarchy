using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HierarchyStyler
{
    /// <summary>
    /// Main manager window. Lists all styles, lets you create / edit / delete them,
    /// and shows a per-scene summary of which entries use which style.
    /// </summary>
    public class HierarchyStylerWindow : EditorWindow
    {
        private Vector2 _scrollStyles;
        private Vector2 _scrollEntries;
        private int _selectedTab;
        private static readonly string[] _tabs = { "Styles", "Scene Entries" };

        [MenuItem("Tools/Hierarchy", priority = 100)]
        public static void Open()
        {
            var win = GetWindow<HierarchyStylerWindow>("Hierarchy Styler");
            win.minSize = new Vector2(360, 400);
            win.Show();
        }

        private void OnEnable()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosed += OnSceneClosed;
        }

        private void OnDisable()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneClosed -= OnSceneClosed;
        }

        private void OnSceneOpened(Scene s, OpenSceneMode m) { Repaint(); }
        private void OnSceneClosed(Scene s) { Repaint(); }

        private void OnGUI()
        {
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabs);
            EditorGUILayout.Space();

            switch (_selectedTab)
            {
                case 0: DrawStylesTab(); break;
                case 1: DrawEntriesTab(); break;
            }
        }

        // ───── Styles tab ─────────────────────────────────────────────────────────

        private void DrawStylesTab()
        {
            var library = HierarchyStyleLibrary.Instance;

            EditorGUILayout.LabelField("Style Library", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                AssetDatabase.GetAssetPath(library), EditorStyles.miniLabel);
            EditorGUILayout.Space();

            _scrollStyles = EditorGUILayout.BeginScrollView(_scrollStyles);

            int removeIndex = -1;
            for (int i = 0; i < library.styles.Count; i++)
            {
                var style = library.styles[i];
                if (style == null) continue;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                var preview = new Rect(GUILayoutUtility.GetRect(30, 18).x,
                    GUILayoutUtility.GetLastRect().y, 30, 18);
                EditorGUI.DrawRect(GUILayoutUtility.GetLastRect(), style.backgroundColor);

                EditorGUI.BeginChangeCheck();
                style.displayName = EditorGUILayout.TextField(style.displayName);
                if (GUILayout.Button("X", GUILayout.Width(22))) removeIndex = i;
                EditorGUILayout.EndHorizontal();

                style.backgroundColor = EditorGUILayout.ColorField("Background", style.backgroundColor);
                style.icon = (Texture2D)EditorGUILayout.ObjectField(
                    "Side Icon", style.icon, typeof(Texture2D), false);
                style.bold = EditorGUILayout.Toggle("Bold Text", style.bold);
                style.textColor = EditorGUILayout.ColorField(
                    new GUIContent("Text Color (alpha 0 = default)"), style.textColor);

                EditorGUILayout.LabelField("ID", style.id, EditorStyles.miniLabel);

                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(library);
                    EditorApplication.RepaintHierarchyWindow();
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(4);
            }

            if (removeIndex >= 0)
            {
                if (EditorUtility.DisplayDialog(
                    "Delete style?",
                    $"Delete style '{library.styles[removeIndex].displayName}'?\n\n" +
                    "Entries in scene databases that referenced this style will be cleaned up automatically.",
                    "Delete", "Cancel"))
                {
                    library.styles.RemoveAt(removeIndex);
                    EditorUtility.SetDirty(library);
                    AssetDatabase.SaveAssets();
                    PruneAllOpenScenes();
                    EditorApplication.RepaintHierarchyWindow();
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ New Style"))
            {
                library.styles.Add(new HierarchyStyle
                {
                    displayName = $"Style {library.styles.Count + 1}"
                });
                EditorUtility.SetDirty(library);
            }
            if (GUILayout.Button("Save"))
            {
                AssetDatabase.SaveAssets();
            }
            EditorGUILayout.EndHorizontal();
        }

        // ───── Entries tab ────────────────────────────────────────────────────────

        private void DrawEntriesTab()
        {
            EditorGUILayout.LabelField("Per-Scene Entries", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Lists every styled entry in currently-loaded scenes. " +
                "Use 'Prune' to drop entries whose target was deleted.",
                MessageType.None);

            EditorGUILayout.Space();

            _scrollEntries = EditorGUILayout.BeginScrollView(_scrollEntries);

            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                var scene = SceneManager.GetSceneAt(s);
                if (!scene.IsValid() || !scene.isLoaded) continue;

                var db = HierarchyStyleDatabase.GetForScene(scene, createIfMissing: false);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(scene.name, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                if (db == null)
                {
                    EditorGUILayout.LabelField("(no database)", EditorStyles.miniLabel,
                        GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    GUILayout.Space(4);
                    continue;
                }

                if (GUILayout.Button("Prune", GUILayout.Width(60)))
                {
                    int removed = db.Prune(HierarchyStyleLibrary.Instance);
                    AssetDatabase.SaveAssets();
                    EditorApplication.RepaintHierarchyWindow();
                    Debug.Log($"[HierarchyStyler] Pruned {removed} entries from scene '{scene.name}'.");
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField($"{db.entries.Count} entries",
                    EditorStyles.miniLabel);

                int removeIdx = -1;
                for (int i = 0; i < db.entries.Count; i++)
                {
                    var e = db.entries[i];
                    var style = HierarchyStyleLibrary.Instance.GetById(e.styleId);

                    EditorGUILayout.BeginHorizontal();

                    string targetName = "<missing>";
                    GameObject target = null;
                    if (GlobalObjectId.TryParse(e.globalObjectId, out var gid))
                    {
                        target = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid)
                            as GameObject;
                        if (target != null) targetName = target.name;
                    }

                    if (GUILayout.Button(targetName, EditorStyles.linkLabel,
                            GUILayout.Width(140)))
                    {
                        if (target != null)
                        {
                            Selection.activeGameObject = target;
                            EditorGUIUtility.PingObject(target);
                        }
                    }

                    GUILayout.Label("→", GUILayout.Width(14));

                    string styleLabel = style != null ? style.displayName : "<missing>";
                    EditorGUILayout.LabelField(styleLabel);

                    if (GUILayout.Button("X", GUILayout.Width(22))) removeIdx = i;
                    EditorGUILayout.EndHorizontal();
                }

                if (removeIdx >= 0)
                {
                    db.entries.RemoveAt(removeIdx);
                    EditorUtility.SetDirty(db);
                    AssetDatabase.SaveAssets();
                    EditorApplication.RepaintHierarchyWindow();
                }

                EditorGUILayout.EndVertical();
                GUILayout.Space(4);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            if (GUILayout.Button("Prune All Open Scenes"))
            {
                PruneAllOpenScenes();
            }
        }

        private static void PruneAllOpenScenes()
        {
            int total = 0;
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                var scene = SceneManager.GetSceneAt(s);
                if (!scene.IsValid() || !scene.isLoaded) continue;
                var db = HierarchyStyleDatabase.GetForScene(scene, createIfMissing: false);
                if (db != null) total += db.Prune(HierarchyStyleLibrary.Instance);
            }
            AssetDatabase.SaveAssets();
            EditorApplication.RepaintHierarchyWindow();
            Debug.Log($"[HierarchyStyler] Pruned {total} stale entries.");
        }
    }
}
