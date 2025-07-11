using UnityEngine;

/// <summary>
/// This information is the source for SelectableRenderInfo
/// </summary>
[CreateAssetMenu(fileName = "EmptyGrabbableInfo", menuName = "Grabbable/GrabbableInfo")]
public class SelectableInfo : ScriptableObject 
{
    public string DisplayName;
    public Sprite Icon;
    public string[] DescriptionLines;
    public Color color;
    public Color selectedColor;
}