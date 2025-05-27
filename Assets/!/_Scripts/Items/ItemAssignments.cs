using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EmptyItemAssignment", menuName = "Items/Item Assignments")]
public class ItemAssignments : ScriptableObject 
{
    [SerializeField]
    private List<ItemAssignment> assignments;
    private Dictionary<Item, ItemInfo> assignmentsSorted;

    private void EnsureAssignmentsSorted() 
    {
        if(assignmentsSorted == null) {
            assignmentsSorted = new();

            foreach(ItemAssignment assignment in assignments) {
                if(assignmentsSorted.ContainsKey(assignment.item)) {
                    Debug.LogError($"Item info for \"{assignment.item}\" has already been set as {assignmentsSorted[assignment.item]} but its set to target {assignment.info} as well.");
                    continue;
                }

                assignmentsSorted.Add(assignment.item, assignment.info);
            }
        } 
    }

    public ItemInfo Get(Item item) 
    {
        EnsureAssignmentsSorted();

        if(!assignmentsSorted.ContainsKey(item)) {
            Debug.LogError($"Can't get assignment for item \"{item}\" it is not in the sorted dictionary.");
            return null;
        }

        return assignmentsSorted[item];
    }

    public bool Contains(Item item) 
    {
        EnsureAssignmentsSorted();

        return assignmentsSorted.ContainsKey(item);
    }

    [Serializable]
    public struct ItemAssignment 
    {
        public Item item;
        public ItemInfo info;
    }
}