using System;
using System.Collections;
using System.Collections.Generic;
using EMullen.Core;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Unity.VisualScripting;
using UnityEngine;

public class GrabbableItem : NetworkBehaviour, IGrabbable
{
    [SerializeField]
    private ItemAssignments itemAssignments;
    [SerializeField]
    private bool useSpawnAs = false;
    [SerializeField]
    private Item spawnAs;
    private bool hasSetItem;

    public readonly SyncVar<Item> item = new();

    private void Start()
    {
        item.OnChange += Item_OnChange;
    }

    private void OnDestroy()
    {
        item.OnChange += Item_OnChange;        
    }

    private void Update()
    {
        if(!InstanceFinder.IsServerStarted)
            return;
        
        if(!hasSetItem && useSpawnAs) {
            hasSetItem = true;
            item.Value = spawnAs;
        }
    }

    public void GrabbedItem() 
    {
        if(!InstanceFinder.IsServerStarted) {
            ServerRPCGrabbedItem();
            return;
        }

        InstanceFinder.ServerManager.Despawn(gameObject);
    }
    [ServerRpc(RequireOwnership = false)]
    private void ServerRPCGrabbedItem() => GrabbedItem();

    public void SetItem(Item item) 
    {
        if(!InstanceFinder.IsServerStarted) {
            SetItem(item);
            return;
        }

        this.item.Value = item;
    }
    [ServerRpc(RequireOwnership = true)]
    public void ServerRPCSetItem(Item item) => SetItem(item);

    private void Item_OnChange(Item prev, Item next, bool asServer)
    {
        GetComponentInChildren<ItemMeshRenderer>().ShowItemMesh(next);
    }

    public Dictionary<string, string> GetVariables() => new();

    public GrabbableInfo OverrideGrabbableInfo() 
    {
        if(!itemAssignments.Contains(item.Value))
            return null;
        
        return itemAssignments.Get(item.Value);
    }
}
