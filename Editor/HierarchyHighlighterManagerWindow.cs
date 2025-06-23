using UnityEngine;
using UnityEditor;

public class HierarchyHighlighterManagerWindow : EditorWindow
{
    private SerializedObject _serializedData;
    private SerializedProperty _styles;
    private HierarchyObjectsData _data; 
    private string _lastPrefixBeforeEdit = "";


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
        if (_data != null) return;

        // Ensure the static constructor runs so asset gets created
        System.Type type = typeof(HierarchyHighlighter);
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(type.TypeHandle);

        // Look next to HierarchyHighlighter.cs
        string[] scriptGuids = AssetDatabase.FindAssets("HierarchyHighlighter t:MonoScript");
        if (scriptGuids.Length == 0)
        {
            Debug.LogError("Cannot locate HierarchyHighlighter.cs.");
            return;
        }

        string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuids[0]);
        string folder = System.IO.Path.GetDirectoryName(scriptPath);
        string expectedAssetPath = System.IO.Path.Combine(folder, "HierarchyObjectsData.asset").Replace("\\", "/");

        _data = AssetDatabase.LoadAssetAtPath<HierarchyObjectsData>(expectedAssetPath);

        if (_data == null)
        {
            Debug.LogError($"HierarchyObjectsData.asset not found at expected path: {expectedAssetPath}");
            return;
        }

        _serializedData = new SerializedObject(_data);
        _styles = _serializedData.FindProperty("Styles");
    }

    private void OnGUI()
    {
        if (_serializedData == null || _styles == null)
        {
            if(_serializedData == null)
            {
                Debug.Log("1");
            }
            if (_styles == null)
            {
                Debug.Log("2");
            }

            EditorGUILayout.HelpBox("Could not load HierarchyObjectsData.", MessageType.Error);
            return;
        }

        _serializedData.Update();

        for (int i = 0; i < _styles.arraySize; i++)
        {
            SerializedProperty element = _styles.GetArrayElementAtIndex(i);
            SerializedProperty prefix = element.FindPropertyRelative("Prefix");

            SerializedProperty color = element.FindPropertyRelative("Color");

            SerializedProperty icon = element.FindPropertyRelative("Icon");
            SerializedProperty align = element.FindPropertyRelative("Alignment");
            SerializedProperty tooltip = element.FindPropertyRelative("TooltipText");

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Style {i + 1}", EditorStyles.boldLabel);
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
            {
                _styles.DeleteArrayElementAtIndex(i);
                break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical();

            bool isDuplicate = IsDuplicatePrefix(prefix.stringValue, i);

            // Store original if editing starts
            if (GUI.GetNameOfFocusedControl() != $"prefix_{i}")
            {
                _lastPrefixBeforeEdit = prefix.stringValue;
            }

            // Draw prefix with red highlight if duplicated
            EditorGUI.BeginChangeCheck();

            GUI.SetNextControlName($"prefix_{i}");
            Rect prefixRect = EditorGUILayout.GetControlRect();
            if (isDuplicate)
            {
                Color prevColor = GUI.color;
                GUI.color = Color.red;
                prefix.stringValue = EditorGUI.TextField(prefixRect, "Prefix", prefix.stringValue);
                GUI.color = prevColor;
            }
            else
            {
                prefix.stringValue = EditorGUI.TextField(prefixRect, "Prefix", prefix.stringValue);
            }

            if (EditorGUI.EndChangeCheck())
            {
                // If changed to a duplicate, wait for deselection to revert
                if (IsDuplicatePrefix(prefix.stringValue, i))
                {
                    // Only revert after field loses focus
                    if (GUI.GetNameOfFocusedControl() != $"prefix_{i}")
                    {
                        prefix.stringValue = _lastPrefixBeforeEdit;
                    }
                }
                else
                {
                    _lastPrefixBeforeEdit = prefix.stringValue;
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.PropertyField(color);
            EditorGUILayout.PropertyField(icon);
            EditorGUILayout.PropertyField(align);
            EditorGUILayout.PropertyField(tooltip);

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Add New Style"))
        {
            _styles.InsertArrayElementAtIndex(_styles.arraySize);
        }

        _serializedData.ApplyModifiedProperties();
    }

    private bool IsDuplicatePrefix(string prefixValue, int currentIndex)
    {
        if (string.IsNullOrWhiteSpace(prefixValue) || prefixValue.Length < 3)
            return false;

        for (int i = 0; i < _styles.arraySize; i++)
        {
            if (i == currentIndex) continue;
            var otherPrefix = _styles.GetArrayElementAtIndex(i).FindPropertyRelative("Prefix").stringValue;
            if (otherPrefix == prefixValue)
                return true;
        }
        return false;
    }

}
