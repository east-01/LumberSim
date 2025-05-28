using UnityEngine;

[CreateAssetMenu(fileName = "EmptyGrabbableInfo", menuName = "Grabbable/GrabbableInfo")]
public class GrabbableInfo : ScriptableObject 
{
    public string DisplayName;
    public Sprite Icon;
    public string[] DescriptionLines;
    /// <summary>
    /// Multiple outline colors can be provided, have multiple array forms:
    /// - Color[1] { single_color }
    /// - Color[2] { selected_color, deselected_color } 
    /// </summary>
    public Color[] OutlineColors;
}