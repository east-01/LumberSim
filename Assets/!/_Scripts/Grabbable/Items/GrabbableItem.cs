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

public class GrabbableItem : NetworkBehaviour, IGrabbable, IS3
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

        GeneralPlayerData gpd = pd.GetData<GeneralPlayerData>();
        bool isPurchasing = false;
        if(Owner != grabber) {
            float cost = itemAssignments.Get(item.Value).cost;
            if(gpd.balance >= cost) {
                gpd.balance = gpd.balance - cost;
                pd.SetData(gpd);
                isPurchasing = true;
            } else 
                return;
        }

        inventoryData.AddItemToHotbar(item.Value);

        pd.SetData(inventoryData);

        if(!isPurchasing)
            InstanceFinder.ServerManager.Despawn(gameObject);
        else
            gameplayManager.PlayerObjectManager.GetPlayer(uid).GetNetworkedAudioController().PlaySound("purchased");
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

    public Dictionary<string, string> GetVariables() => new();
    public bool CanPickup(NetworkConnection pickupConnection, string uid, out string reason) 
    {
        reason = $"Item owned by {Owner}";
        return pickupConnection == Owner;
    } 
    public GrabbableInfo OverrideGrabbableInfo() => itemAssignments.Contains(item.Value) ? itemAssignments.Get(item.Value) : null;
    public GameObject GetOutlineObject() => GetComponentInChildren<ItemMeshRenderer>().CurrentItemMesh;

    public GrabbableRenderArgs? Render(string viewingPlayer = null)
    {
        if(!itemAssignments.Contains(item.Value))
            throw new InvalidOperationException($"Can't render item \"{item.Value}\" it is not in item assignments.");

        ItemInfo info = itemAssignments.Get(item.Value);
        GrabbableRenderArgs args = GrabbableRenderArgs.DefaultRenderArgs(info);
        List<string> descriptionLines = args.descriptionLines.ToList();

        if(!Owner.IsValid) {
            descriptionLines.Add("");
            descriptionLines.Add($"[E] Buy: <color=green>${info.cost}</color>");
        } else if(Owner != InstanceFinder.ClientManager.Connection) {
            descriptionLines.Add("");
            descriptionLines.Add($"Owner: {OwnerId}");
        }

        args.descriptionLines = descriptionLines.ToArray();

        return args;
    }
}
