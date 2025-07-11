using System;
using System.Collections.Generic;
using EMullen.Core;
using UnityEngine;

[ExecuteInEditMode]
public class ItemMeshRenderer : MonoBehaviour 
{
    [SerializeField]
    private Item currentItem;
    [SerializeField]
    private List<ItemMeshInfo> itemMeshInfos;
    private Dictionary<Item, ItemMeshInfo> itemMeshInfosDict;
    [SerializeField]
    private Transform spawnPoint;

    public GameObject CurrentItemMesh { get; private set; }

    private void LoadDict() 
    {
        itemMeshInfosDict = new();
        foreach(ItemMeshInfo info in itemMeshInfos) {
            if(itemMeshInfosDict.ContainsKey(info.item)) {
                Debug.LogWarning($"Can't add ItemMeshInfo named \"{info.item}\" it is already cached.");
                continue;
            }

            itemMeshInfosDict.Add(info.item, info);
        }  
    }

    public void ShowItemMesh(Item item, bool allowCollider = true) 
    {
        if(itemMeshInfosDict == null)
            LoadDict();

        if(item == Item.NONE) {
            if(spawnPoint.gameObject.activeSelf)
                spawnPoint.gameObject.SetActive(false);
            return;
        }

        spawnPoint.gameObject.SetActive(true);

        if(item == currentItem && CurrentItemMesh != null)
            return;

        if(CurrentItemMesh != null) {
            Destroy(CurrentItemMesh); // TODO: Maybe cache these?
            CurrentItemMesh = null;
        }

        currentItem = item;
        if(item == Item.NONE)
            return;

        if(!itemMeshInfosDict.ContainsKey(item))
            throw new InvalidOperationException($"Can't render item mesh \"{item}\" it is not cached. Version mismatch? Current cached keys: {string.Join(", ", Enum.GetValues(typeof(Item)))}");

        ItemMeshInfo info = itemMeshInfosDict[item];

        CurrentItemMesh = Instantiate(info.itemMeshPrefab, spawnPoint);

        if(!allowCollider && CurrentItemMesh.TryGetComponent(out Collider collider))
            collider.enabled = false;

        currentItem = item;
    }

}

[Serializable]
public struct ItemMeshInfo 
{
    public Item item;
    public GameObject itemMeshPrefab;
}