using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EMullen.Core;
using EMullen.PlayerMgmt;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Unity.VisualScripting;
using UnityEngine;

public class GrabbableItem : NetworkBehaviour, IGrabbable, ISelectableController, IS3
{
    [SerializeField]
    private ItemAssignments itemAssignments;
    [SerializeField]
    private bool useSpawnAs = false;
    [SerializeField]
    private Item spawnAs;
    private bool hasSetItem;

    public readonly SyncVar<Item> item = new();
    [SerializeField]
    private Item itemReadout;

    private GameplayManager gameplayManager;

    private void Awake()
    {
        item.OnChange += Item_OnChange;

        GetComponent<Selectable>().selectableController = this;   
    }
    
    public override void OnStartClient() 
    {
        base.OnStartClient();
        UpdateItemMesh();
    }

    private void OnDestroy()
    {
        item.OnChange -= Item_OnChange;        
    }

    public void SingletonRegistered(Type type, object singleton)
    {
        if(type != typeof(GameplayManager))
            return;

        gameplayManager = singleton as GameplayManager;
    }

    public void SingletonDeregistered(Type type, object singleton)
    {
        if(type != typeof(GameplayManager))
            return;

    }

    private void Update()
    {
        itemReadout = item.Value;

        // Safely subscribe to the GameplayManager singleton
        if(gameObject.scene.name == "GameplayScene") {
            SceneLookupData lookupData = gameObject.scene.GetSceneLookupData();

            if(!SceneSingletons.IsSubscribed(this, lookupData, typeof(GameplayManager))) {
                SceneSingletons.SubscribeToSingleton(this, lookupData, typeof(GameplayManager));
            }
        }

        if(!InstanceFinder.IsServerStarted)
            return;
        
        if(!hasSetItem && useSpawnAs) {
            hasSetItem = true;
            item.Value = spawnAs;
        }
    }

    public virtual void GrabbedItem(NetworkConnection grabber, string uid) 
    {
        if(!InstanceFinder.IsServerStarted) {
            ServerRPCGrabbedItem(grabber, uid);
            return;
        }

        // Add item to hotbar, if not successful break early
        PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(uid);
        pd.EnsureLumberData();

        InventoryData inventoryData = pd.GetData<InventoryData>();
        if(!inventoryData.CanAddItemToHotbar())
            return;

        inventoryData.AddItemToHotbar(item.Value);
        pd.SetData(inventoryData);

        InstanceFinder.ServerManager.Despawn(gameObject);
    }
    [ServerRpc(RequireOwnership = false)]
    private void ServerRPCGrabbedItem(NetworkConnection grabber, string uid) => GrabbedItem(grabber, uid);

    public void SetItem(Item item) 
    {
        if(!InstanceFinder.IsServerStarted) {
            ServerRPCSetItem(item);
            return;
        }

        this.item.Value = item;
    }
    [ServerRpc(RequireOwnership = true)]
    public void ServerRPCSetItem(Item item) => SetItem(item);

    private void Item_OnChange(Item prev, Item next, bool asServer)
    {
        UpdateItemMesh();
    }

    public void UpdateItemMesh() => GetComponentInChildren<ItemMeshRenderer>().ShowItemMesh(item.Value);

    public bool CanPickup(NetworkConnection pickupConnection, string uid, out string reason) 
    {
        reason = $"Item owned by {Owner}";
        return pickupConnection == Owner;
    } 

    public GameObject GetOutlineObject() => GetComponentInChildren<ItemMeshRenderer>().CurrentItemMesh;
    public SelectableRenderInfo GetSelectableInfo()
    {
        ItemInfo itemInfo = itemAssignments.Get(item.Value);
        SelectableRenderInfo info = SelectableRenderInfo.DefaultRenderArgs(itemInfo);

        if(gameplayManager == null)
            return info;

        List<string> descriptionLines = info.descriptionLines.ToList();

        Player player = gameplayManager.Player;
        if(!player.ProgressionManager.CanUseItem(item.Value)) {

            descriptionLines.Add("");
            descriptionLines.Add($"<color=red>Requires objectives:</color>");            
            List<ProgressionPoint> reqProgPoints = player.ProgressionManager.ItemRequiredProgressionPoints(item.Value);
            reqProgPoints.ForEach(pp => descriptionLines.Add($"<color=red> - {pp.progressionDisplayName}</color>"));

            info.selectColor = Color.red;
            info.color = Color.red - new Color(0.25f, 0.25f, 0.25f, 0f);
            info.color.a = 1f;

        } else if(!Owner.IsValid) {

            descriptionLines.Add("");
            descriptionLines.Add($"[E] Buy: <color=green>${itemInfo.cost}</color>");

        } else {

            descriptionLines.Add("");
            descriptionLines.Add($"Owner: {OwnerId}");

        }


        info.descriptionLines = descriptionLines.ToArray();

        return info;
    }
}
