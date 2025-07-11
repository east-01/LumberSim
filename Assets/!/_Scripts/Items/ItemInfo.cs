using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EmptyItemData", menuName = "Items/Item Data")]
public class ItemInfo : SelectableInfo 
{
    public float cost;
    public List<ProgressionPoint> requiredProgressionPoints;
}
