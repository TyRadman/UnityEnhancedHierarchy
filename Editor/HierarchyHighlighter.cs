using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[InitializeOnLoad]
public static class HierarchyHighlighter
{
    public enum NameMode
    {
        Left = 0,
        Right = 1,
        None = 2
    }

    private static HierarchyObjectsData _data;

    static HierarchyHighlighter()
    {
        LoadData();
        EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
    }

    private static void LoadData()
    {
        if (_data != null) return;

        string[] guids = AssetDatabase.FindAssets("t:HierarchyObjectsData");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            _data = AssetDatabase.LoadAssetAtPath<HierarchyObjectsData>(path);
            return;
        }

        // No asset found, create it
        string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(ScriptableObject.CreateInstance<HierarchyObjectsData>()));
        string directory = System.IO.Path.GetDirectoryName(scriptPath);
        string assetPath = System.IO.Path.Combine(directory, "HierarchyObjectsData.asset").Replace("\\", "/");

        _data = ScriptableObject.CreateInstance<HierarchyObjectsData>();
        AssetDatabase.CreateAsset(_data, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Created new HierarchyObjectsData at: {assetPath}");
    }


    private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        if (_data == null) return;

        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null) return;

        string objectName = obj.name;
        char firstChar = char.ToLower(objectName[0]);
        NameMode mode = firstChar == 'l' ? NameMode.Left : firstChar == 'r' ? NameMode.Right : NameMode.None;
        if (mode != NameMode.None)
            objectName = objectName.Substring(1);

        foreach (var style in _data.Styles)
        {
            if (string.IsNullOrWhiteSpace(style.Prefix) || style.Prefix.Length < 3)
            {
                continue;
            }

            if (objectName.StartsWith(style.Prefix))
            {
                string labelText = objectName.Substring(style.Prefix.Length).TrimStart();

                // Draw background
                Rect bgRect = new Rect(0, selectionRect.y, EditorGUIUtility.currentViewWidth, selectionRect.height);

                Color bgColor = style.Color;

                // Adjust alpha based on state
                if (Selection.activeInstanceID == instanceID)
                {
                    bgColor.a = 0f; // fully transparent
                }
                else if (bgRect.Contains(Event.current.mousePosition))
                {
                    bgColor.a *= 0.5f; // 50% opacity
                }

                EditorGUI.DrawRect(bgRect, bgColor);

                // Icon
                const float iconSize = 16f;
                Rect iconRect = new Rect(bgRect.xMax - iconSize - 4, selectionRect.y, iconSize, iconSize);

                // Text
                GUIStyle labelStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = Color.white },
                    alignment = style.Alignment switch
                    {
                        NameMode.Left => TextAnchor.MiddleLeft,
                        NameMode.Right => TextAnchor.MiddleRight,
                        _ => TextAnchor.MiddleLeft
                    },
                    clipping = TextClipping.Clip
                };

                float padding = 4f;
                Rect textRect = new Rect(selectionRect)
                {
                    width = iconRect.x - selectionRect.x - padding
                };

                // GUIContent with tooltip
                GUIContent labelContent = new GUIContent(labelText, style.TooltipText);

                EditorGUI.LabelField(textRect, labelContent, labelStyle);

                if (style.Icon != null)
                {
                    GUI.DrawTexture(iconRect, style.Icon, ScaleMode.ScaleToFit);
                }

                break;
            }
        }
    }
}
