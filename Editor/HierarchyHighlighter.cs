using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Xml.Linq;

[InitializeOnLoad]
public static class HierarchyHighlighter
{
    public enum NameMode
    {
        Left = 0,
        Right = 1,
        None = 2
    }

    private static readonly Dictionary<string, string> prefixColors = new()
    {
        { "---", "#f95738" },
        { "===", "#ee964b" },
        { "###", "#F4D35E" },
        { "___", "#FAF0CA" },
        { "///", "#0D3B66" },
    };

    static HierarchyHighlighter()
    {
        EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
    }

    private static void HandleHierarchyWindowItemOnGUI(int instanceID, Rect selectionRect)
    {
        GameObject selectedObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        
        if (selectedObject == null)
        {
            return;
        }

        foreach (KeyValuePair<string,string> kvp in prefixColors)
        {
            string objectName = selectedObject.name;
            char firstChar = char.ToLower(objectName[0]);
            NameMode mode = firstChar == 'l' ? NameMode.Left : firstChar == 'r' ? NameMode.Right : NameMode.None;

            if(mode != NameMode.None)
            {
                objectName = objectName.Substring(1);
            }

            if (objectName.StartsWith(kvp.Key))
            {
                Rect originalRect = selectionRect;
                selectionRect.x = 0;
                selectionRect.width = EditorGUIUtility.currentViewWidth;
                ColorUtility.TryParseHtmlString(kvp.Value, out Color color);
                EditorGUI.DrawRect(selectionRect, color);

                string name = objectName.Substring(kvp.Key.Length).TrimStart();
                DrawItemName(mode, originalRect, name);
                break;
            }
        }
    }

    private static void DrawItemName(NameMode mode, Rect rect, string name)
    {
        GUIStyle style = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.white } };

        switch (mode)
        {
            case NameMode.Left:
                rect.x = 0;
                rect.width = EditorGUIUtility.currentViewWidth;
                style.alignment = TextAnchor.MiddleLeft;
                EditorGUI.LabelField(rect, name, style);
                break;
            case NameMode.Right:
                style.alignment = TextAnchor.MiddleRight;
                break;
            case NameMode.None:
                break;
        }

        EditorGUI.LabelField(rect, name, style);
    }
}
