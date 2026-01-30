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

        var styles = _data.Styles;

        for (int i = 0; i < styles.Count; i++)
        {
            var style = styles[i];

            if(style == null)
            {
                break;
            }

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();

            bool nameExists = !string.IsNullOrEmpty(style.StyleName);
            string displayName;

            if (nameExists)
            {
                displayName = style.StyleName;
            }
            else
            {
                displayName = $"Style {i + 1}";
                style.StyleName = $"Style {i + 1}";
            }

            if (_editingStyleNameIndex == i)
            {
                style.StyleName = EditorGUILayout.TextField(style.StyleName);

                if (GUILayout.Button("Save name", GUILayout.Width(80)))
                {
                    _editingStyleNameIndex = -1;
                    EditorUtility.SetDirty(_data);
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
                styles.Remove(style);
                break;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginVertical();

            bool isDuplicate = IsDuplicatePrefix(style.Prefix, i);

            // Store original if editing starts
            if (GUI.GetNameOfFocusedControl() != $"prefix_{i}")
            {
                _lastPrefixBeforeEdit = style.Prefix;
            }

            // Draw prefix with red highlight if duplicated
            EditorGUI.BeginChangeCheck();

            GUI.SetNextControlName($"prefix_{i}");
            Rect prefixRect = EditorGUILayout.GetControlRect();
            if (isDuplicate)
            {
                Color prevColor = GUI.color;
                GUI.color = Color.red;
                style.Prefix = EditorGUI.TextField(prefixRect, "Prefix", style.Prefix);
                GUI.color = prevColor;
            }
            else
            {
                style.Prefix = EditorGUI.TextField(prefixRect, "Prefix", style.Prefix);
            }

            if (EditorGUI.EndChangeCheck())
            {
                // If changed to a duplicate, wait for deselection to revert
                if (IsDuplicatePrefix(style.Prefix, i))
                {
                    // Only revert after field loses focus
                    if (GUI.GetNameOfFocusedControl() != $"prefix_{i}")
                    {
                        style.Prefix = _lastPrefixBeforeEdit;
                    }
                }
                else
                {
                    _lastPrefixBeforeEdit = style.Prefix;
                }
            }

            EditorGUILayout.EndVertical();

            style.Color = EditorGUILayout.ColorField("Color", style.Color); 
            style.Icon = (Texture2D)EditorGUILayout.ObjectField("Icon", style.Icon, typeof(Texture2D), false, GUILayout.Height(16));
            style.Alignment = (NameMode)EditorGUILayout.EnumPopup("Alignment", style.Alignment);
            style.TooltipText = EditorGUILayout.TextField("Tooltip", style.TooltipText);

            EditorGUILayout.EndVertical();
        }

        if (GUILayout.Button("Add New Style"))
        {
            styles.Add(new HierarchyPrefixStyle());
        }

        _serializedData.ApplyModifiedProperties();
        EditorUtility.SetDirty(_data);
    }

    private bool IsDuplicatePrefix(string prefixValue, int currentIndex)
    {
        if (string.IsNullOrWhiteSpace(prefixValue) || prefixValue.Length < 3)
            return false;

        for (int i = 0; i < _data.Styles.Count; i++)
        {
            if (i == currentIndex) continue;
            var otherPrefix = _data.Styles[i].Prefix;
            if (otherPrefix == prefixValue)
                return true;
        }
        return false;
    }

}
