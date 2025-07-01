using UnityEngine;

[System.Serializable]
public class HierarchyPrefixStyle
{
    public string StyleName;
    public string Prefix;
    public Color Color;
    public Texture2D Icon;
    public NameMode Alignment;
    public string TooltipText;
}

public enum NameMode
{
    None = 0, Left = 1, Right = 2
}