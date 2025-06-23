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

        // Ensure the static constructor runs
        System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(HierarchyHighlighter).TypeHandle);

        // Access _data via reflection (or a public getter)
        _data = typeof(HierarchyHighlighter)
            .GetField("_data", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
            ?.GetValue(null) as HierarchyObjectsData;

        if (_data == null)
        {
            Debug.LogError("[HierarchyHighlighterManagerWindow] Could not find HierarchyObjectsData asset. Make sure it's included in the package.");
            return;
        }

        _serializedData = new SerializedObject(_data);
        _styles = _serializedData.FindProperty("Styles");
    }


    private void OnGUI()
    {
        if (_serializedData == null || _styles == null)
        {
            LoadData();
        
            if (_serializedData == null)
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
