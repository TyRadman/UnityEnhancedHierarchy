using UnityEngine;
using UnityEditor;

public class HierarchyHighlighterManagerWindow : EditorWindow
{
    private SerializedObject _serializedData;
    private HierarchyObjectsData _data;
    private string _lastPrefixBeforeEdit = "";
    private int _editingStyleNameIndex = -1;

    [MenuItem("Tools/Hierarchy Highlighter Manager")]
    public static void ShowWindow()
    {
        GetWindow<HierarchyHighlighterManagerWindow>("Hierarchy Highlighter");
    }

    private void OnEnable()
    {
        LoadData();
    }

    private void LoadData()
    {
        if (_data != null && _serializedData != null)
        {
            return;
        }

        HierarchyHighlighter.LoadData();

        _data = HierarchyHighlighter.Data;

        if (_data)
        {
            HierarchyHighlighter.LoadData();
        }

        if (_data == null)
        {
            Debug.LogError("FATAL: [HierarchyHighlighterManagerWindow] Could not find HierarchyObjectsData asset. Make sure it's included in the package.");
            return;
        }

        _serializedData = new SerializedObject(_data);
    }

    private void OnGUI()
    {
        if (_serializedData == null)
        {
            LoadData();

            if (_serializedData == null)
            {
                Debug.Log("1");
            }

            EditorGUILayout.HelpBox("Could not load HierarchyObjectsData.", MessageType.Error);
            return;
        }

        _serializedData.Update();

        SerializedProperty stylesProp = _serializedData.FindProperty("Styles");

        for (int i = 0; i < stylesProp.arraySize; i++)
        {
            SerializedProperty styleElement = stylesProp.GetArrayElementAtIndex(i);
            
            SerializedProperty styleNameProp = styleElement.FindPropertyRelative("StyleName");
            SerializedProperty prefixProp = styleElement.FindPropertyRelative("Prefix");
            SerializedProperty colorProp = styleElement.FindPropertyRelative("Color");
            SerializedProperty iconProp = styleElement.FindPropertyRelative("Icon");
            SerializedProperty alignmentProp = styleElement.FindPropertyRelative("Alignment");
            SerializedProperty tooltipProp = styleElement.FindPropertyRelative("TooltipText");

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            bool nameExists = !string.IsNullOrEmpty(styleNameProp.stringValue);
            string displayName;

            if (nameExists)
            {
                displayName = styleNameProp.stringValue;
            }
            else
            {
                displayName = $"Style {i + 1}";
                styleNameProp.stringValue = $"Style {i + 1}";
            }

            if (_editingStyleNameIndex == i)
            {
                styleNameProp.stringValue = EditorGUILayout.TextField(styleNameProp.stringValue);

                if (GUILayout.Button("Save name", GUILayout.Width(80)))
                {
                    _editingStyleNameIndex = -1;
                    _serializedData.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();
                }
            }
            else
            {
                EditorGUILayout.LabelField(displayName, EditorStyles.boldLabel);

                if (GUILayout.Button("Edit name", GUILayout.Width(80)))
                {
                    _editingStyleNameIndex = i;
                }
            }

            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                // Just to close the others opened above
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                stylesProp.DeleteArrayElementAtIndex(i);
                break;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical();

            bool isDuplicate = IsDuplicatePrefix(prefixProp.stringValue, i);

            // Store original if editing starts
            if (GUI.GetNameOfFocusedControl() != $"prefix_{i}")
            {
                _lastPrefixBeforeEdit = prefixProp.stringValue;
            }

            // Draw prefix with red highlight if duplicated
            EditorGUI.BeginChangeCheck();

            GUI.SetNextControlName($"prefix_{i}");
            Rect prefixRect = EditorGUILayout.GetControlRect();
            if (isDuplicate)
            {
                Color prevColor = GUI.color;
                GUI.color = Color.red;
                prefixProp.stringValue = EditorGUI.TextField(prefixRect, "Prefix", prefixProp.stringValue);
                GUI.color = prevColor;
            }
            else
            {
                prefixProp.stringValue = EditorGUI.TextField(prefixRect, "Prefix", prefixProp.stringValue);
            }

            if (EditorGUI.EndChangeCheck())
            {
                // If changed to a duplicate, wait for deselection to revert
                if (IsDuplicatePrefix(prefixProp.stringValue, i))
                {
                    // Only revert after field loses focus
                    if (GUI.GetNameOfFocusedControl() != $"prefix_{i}")
                    {
                        prefixProp.stringValue = _lastPrefixBeforeEdit;
                    }
                }
                else
                {
                    _lastPrefixBeforeEdit = prefixProp.stringValue;
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.PropertyField(colorProp, new GUIContent("Color"));
            EditorGUILayout.PropertyField(iconProp, new GUIContent("Icon"), true, GUILayout.Height(16));
            EditorGUILayout.PropertyField(alignmentProp, new GUIContent("Alignment"));
            EditorGUILayout.PropertyField(tooltipProp, new GUIContent("Tooltip"));

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Add New Style"))
        {
            stylesProp.InsertArrayElementAtIndex(stylesProp.arraySize);
        }

        _serializedData.ApplyModifiedProperties();
    }

    private bool IsDuplicatePrefix(string prefixValue, int currentIndex)
    {
        if (string.IsNullOrWhiteSpace(prefixValue) || prefixValue.Length < 3)
            return false;

        SerializedProperty stylesProp = _serializedData.FindProperty("Styles");
        for (int i = 0; i < stylesProp.arraySize; i++)
        {
            if (i == currentIndex) continue;
            var otherPrefix = stylesProp.GetArrayElementAtIndex(i).FindPropertyRelative("Prefix").stringValue;
            if (otherPrefix == prefixValue)
                return true;
        }
        return false;
    }
}
