using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

[InitializeOnLoad]
public static class HierarchyHighlighter
{
    public static HierarchyObjectsData Data;

    static HierarchyHighlighter()
    {
        LoadData();
        EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
    }

    public static void LoadData()
    {
        if (Data != null) 
        {
            return;
        }
        
        string dirPath = "Assets/HierarchyHighlighter/Settings";
        string assetPath = $"{dirPath}/HierarchyObjectsData.asset";

        Data = AssetDatabase.LoadAssetAtPath<HierarchyObjectsData>(assetPath);

        if (Data == null)
        {
            Debug.Log("[HierarchyHighlighter] Creating new HierarchyObjectsData asset...");

            if (!AssetDatabase.IsValidFolder(dirPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Editor Default Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Editor Default Resources");
                }
            }

            Data = ScriptableObject.CreateInstance<HierarchyObjectsData>();
            
            AssetDatabase.CreateAsset(Data, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        if (Data == null) return;

        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null) return;

        string objectName = obj.name;
        char firstChar = char.ToLower(objectName[0]);
        NameMode mode = firstChar == 'l' ? NameMode.Left : firstChar == 'r' ? NameMode.Right : NameMode.None;
        if (mode != NameMode.None)
            objectName = objectName.Substring(1);

        foreach (var style in Data.Styles)
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


                float brightnessBoost = Selection.activeInstanceID == instanceID ? 1.4f :
                        bgRect.Contains(Event.current.mousePosition) ? 1.15f : 1f;

                bgColor.r = Mathf.Min(bgColor.r * brightnessBoost, 1f);
                bgColor.g = Mathf.Min(bgColor.g * brightnessBoost, 1f);
                bgColor.b = Mathf.Min(bgColor.b * brightnessBoost, 1f);


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
