using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HierarchyStyler
{
    /// <summary>
    /// One mapping entry: a GameObject identified by GlobalObjectId.ToString() -> style id.
    /// </summary>
    [Serializable]
    public class HierarchyStyleEntry
    {
        public string globalObjectId; // GlobalObjectId.ToString() of the target GameObject
        public string styleId;        // id from HierarchyStyleLibrary
    }

    /// <summary>
    /// Per-scene database. Saved as an asset named "<SceneName>_HierarchyStyles.asset"
    /// in the same folder as the scene so it follows the scene around.
    /// </summary>
    public class HierarchyStyleDatabase : ScriptableObject
    {
        public List<HierarchyStyleEntry> entries = new List<HierarchyStyleEntry>();

        // Cached per-loaded-scene instance map: scenePath -> database.
        private static readonly Dictionary<string, HierarchyStyleDatabase> _cache =
            new Dictionary<string, HierarchyStyleDatabase>();

        public static HierarchyStyleDatabase GetForScene(Scene scene, bool createIfMissing = false)
        {
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
                return null;

            if (_cache.TryGetValue(scene.path, out var cached) && cached != null)
                return cached;

            string assetPath = GetAssetPathForScene(scene);
            var db = AssetDatabase.LoadAssetAtPath<HierarchyStyleDatabase>(assetPath);

            if (db == null && createIfMissing)
            {
                db = CreateInstance<HierarchyStyleDatabase>();
                string dir = Path.GetDirectoryName(assetPath);
                if (!string.IsNullOrEmpty(dir) && !AssetDatabase.IsValidFolder(dir))
                {
                    // Scenes always live in an existing folder, but be safe.
                    Directory.CreateDirectory(dir);
                    AssetDatabase.Refresh();
                }
                AssetDatabase.CreateAsset(db, assetPath);
                AssetDatabase.SaveAssets();
            }

            if (db != null) _cache[scene.path] = db;
            return db;
        }

        private static string GetAssetPathForScene(Scene scene)
        {
            string dir = Path.GetDirectoryName(scene.path);
            string sceneName = Path.GetFileNameWithoutExtension(scene.path);
            return $"{dir}/{sceneName}_HierarchyStyles.asset".Replace("\\", "/");
        }

        public static void InvalidateCache()
        {
            _cache.Clear();
        }

        public string GetStyleIdFor(string globalObjectId)
        {
            if (string.IsNullOrEmpty(globalObjectId)) return null;
            for (int i = 0; i < entries.Count; i++)
                if (entries[i].globalObjectId == globalObjectId) return entries[i].styleId;
            return null;
        }

        public void SetStyle(string globalObjectId, string styleId)
        {
            if (string.IsNullOrEmpty(globalObjectId)) return;
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].globalObjectId == globalObjectId)
                {
                    entries[i].styleId = styleId;
                    EditorUtility.SetDirty(this);
                    return;
                }
            }
            entries.Add(new HierarchyStyleEntry { globalObjectId = globalObjectId, styleId = styleId });
            EditorUtility.SetDirty(this);
        }

        public void RemoveStyle(string globalObjectId)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                if (entries[i].globalObjectId == globalObjectId)
                {
                    entries.RemoveAt(i);
                    EditorUtility.SetDirty(this);
                }
            }
        }

        /// <summary>
        /// Drop entries whose target GameObject no longer exists, or whose style id is unknown.
        /// </summary>
        public int Prune(HierarchyStyleLibrary library)
        {
            int removed = 0;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var e = entries[i];
                bool styleExists = library != null && library.GetById(e.styleId) != null;
                bool targetExists = false;

                if (GlobalObjectId.TryParse(e.globalObjectId, out var gid))
                {
                    var obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(gid);
                    targetExists = obj != null;
                }

                if (!styleExists || !targetExists)
                {
                    entries.RemoveAt(i);
                    removed++;
                }
            }
            if (removed > 0) EditorUtility.SetDirty(this);
            return removed;
        }
    }
}
