using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HierarchyStyler
{
    /// <summary>
    /// A single visual style that can be applied to a hierarchy entry.
    /// </summary>
    [Serializable]
    public class HierarchyStyle
    {
        [Tooltip("Unique identifier. Don't change once assigned to entries.")]
        public string id = Guid.NewGuid().ToString();

        [Tooltip("Display name shown in the picker.")]
        public string displayName = "New Style";

        [Tooltip("Background color drawn behind the entry. Alpha matters.")]
        public Color backgroundColor = new Color(0.2f, 0.4f, 0.8f, 0.5f);

        [Tooltip("Optional icon drawn on the right side of the entry.")]
        public Texture2D icon;

        [Tooltip("If true the entry's name is drawn in bold.")]
        public bool bold;

        [Tooltip("Optional override for the text color. If alpha == 0 the default color is used.")]
        public Color textColor = new Color(1f, 1f, 1f, 0f);
    }

    /// <summary>
    /// Project-wide library of available styles. Stored as a single asset
    /// in the consumer's project (NOT inside the package, since packages are read-only).
    /// </summary>
    public class HierarchyStyleLibrary : ScriptableObject
    {
        private const string AssetFolder = "Assets/HierarchyStyler";
        private const string AssetPath = AssetFolder + "/HierarchyStyleLibrary.asset";

        public List<HierarchyStyle> styles = new List<HierarchyStyle>();

        private static HierarchyStyleLibrary _instance;

        public static HierarchyStyleLibrary Instance
        {
            get
            {
                if (_instance != null) return _instance;

                // Check the canonical path first.
                _instance = AssetDatabase.LoadAssetAtPath<HierarchyStyleLibrary>(AssetPath);

                // Fall back to a project-wide search in case the user moved it.
                if (_instance == null)
                {
                    var guids = AssetDatabase.FindAssets("t:HierarchyStyleLibrary");
                    if (guids.Length > 0)
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                        _instance = AssetDatabase.LoadAssetAtPath<HierarchyStyleLibrary>(path);
                    }
                }

                if (_instance == null) _instance = CreateAsset();
                return _instance;
            }
        }

        private static HierarchyStyleLibrary CreateAsset()
        {
            if (!AssetDatabase.IsValidFolder(AssetFolder))
                AssetDatabase.CreateFolder("Assets", "HierarchyStyler");

            var asset = CreateInstance<HierarchyStyleLibrary>();
            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            return asset;
        }

        public HierarchyStyle GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < styles.Count; i++)
                if (styles[i] != null && styles[i].id == id) return styles[i];
            return null;
        }
    }
}
