using System;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemMeshRenderer))]
public class ItemMeshRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ItemMeshRenderer meshRenderer = (ItemMeshRenderer)target;

        if (GUILayout.Button("Preview Item Mesh"))
        {
            meshRenderer.ShowItemMesh(GetCurrentItem(meshRenderer));
        }
    }

    private Item GetCurrentItem(ItemMeshRenderer renderer)
    {
        // Access the private `currentName` field via serialized property
        SerializedProperty nameProp = serializedObject.FindProperty("currentItem");
        if (!Enum.TryParse(nameProp?.stringValue, out Item item)) {
            Debug.LogError($"Can't get current item from string value \"{nameProp?.stringValue}\"");
            return Item.NONE;
        }

        return item;
    }
}