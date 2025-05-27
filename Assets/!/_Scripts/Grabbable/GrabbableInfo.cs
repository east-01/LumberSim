using UnityEngine;

[CreateAssetMenu(fileName = "EmptyGrabbableInfo", menuName = "Grabbable/GrabbableInfo")]
public class GrabbableInfo : ScriptableObject 
{
    public string DisplayName;
    public Sprite Icon;
    public string[] DescriptionLines;
}