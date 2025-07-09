
using System;
using System.Collections.Generic;
using EMullen.PlayerMgmt;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

/// <summary>
/// Interacts with the GeneralPlayerData's balance value to purchase various things.
/// For items, the player can purchase them to make the player the owner of the object.
/// </summary>
public class PlayerTransactionManager : NetworkBehaviour 
{
    [SerializeField]
    private ItemAssignments itemAssignments;

    private Player player;

    private void Awake()
    {
        player = GetComponent<Player>();   
    }

    public void PurchaseGrabbableItem(NetworkObject grabbableItem) 
    {
        if(grabbableItem == null)
            throw new InvalidOperationException($"Tried to purchase GrabbableItem, NetworkObject is null.");
        if(!grabbableItem.TryGetComponent(out GrabbableItem item))
            throw new InvalidOperationException($"Tried to purchase NetworkObject named \"{grabbableItem.name}\" as a GrabbableItem, no GrabbableItem found.");

        // Perform checks on source of grab
        if(grabbableItem.Owner == LocalConnection) {
            Debug.LogError("Can't purchase grabbable item, already owner of it.");
            return;
        }

        PlayerData pd = player.PlayerData;
        GeneralPlayerData gpd = pd.GetData<GeneralPlayerData>();
        if(gpd.balance < itemAssignments.Get(item.item.Value).cost) {
            player.ShowHUDWarning($"Can't afford", 2f);
            return;
        }

       InventoryData inventoryData = pd.GetData<InventoryData>();
        if(!inventoryData.CanAddItemToHotbar()) {
            player.ShowHUDWarning($"No space", 2f);
            return;
        }

        PerformPurchase(LocalConnection, player.uid.Value, grabbableItem);
    }

    private void PerformPurchase(NetworkConnection purchaseConn, string purchaseUID, NetworkObject grabbableItem) 
    {
        if(grabbableItem == null)
            throw new InvalidOperationException($"Tried to purchase GrabbableItem, NetworkObject is null.");
        if(!grabbableItem.TryGetComponent(out GrabbableItem item))
            throw new InvalidOperationException($"Tried to purchase NetworkObject named \"{grabbableItem.name}\" as a GrabbableItem, no GrabbableItem found.");

        if(!InstanceFinder.IsServerStarted) {
            ServerRPCPerformPurchase(purchaseConn, purchaseUID, grabbableItem);
            return;
        }

        float cost = itemAssignments.Get(item.item.Value).cost;

        PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(purchaseUID);
        GeneralPlayerData gpd = pd.GetData<GeneralPlayerData>();
        if(gpd.balance < cost) {
            Debug.LogError($"Player tried to purhcase item \"{item.item.Value}\" but couldn't afford.");
            return;
        }

        InventoryData inventoryData = pd.GetData<InventoryData>();
        if(!inventoryData.CanAddItemToHotbar()) {
            Debug.LogError($"Player tried to purhcase item \"{item.item.Value}\" but had no room.");
            return;
        }

        // Perform data changes
        inventoryData.AddItemToHotbar(item.item.Value);
        pd.SetData(inventoryData);

        gpd.balance -= cost;
        pd.SetData(gpd);

        player.NetworkedAudioController.PlaySound("purchased");

        // Update the progression for buying a type of item
        UpdateProgression_ItemPurchased(pd, item.item.Value);

    }
    [ServerRpc(RequireOwnership = false)]
    private void ServerRPCPerformPurchase(NetworkConnection purhcaseConn, string purchaseUID, NetworkObject grabbableItem) => PerformPurchase(purhcaseConn, purchaseUID, grabbableItem);

    private void UpdateProgression_ItemPurchased(PlayerData pd, Item item) 
    {
        pd.EnsureLumberData();
        ProgressionData progression = pd.GetData<ProgressionData>();

        Dictionary<Item, string> itemsMetricNames = new() {
            { Item.AXE_T1, MetricNames.BOUGHT_TONE_AXE },
            { Item.AXE_T2, MetricNames.BOUGHT_TTWO_AXE },
            { Item.AXE_T3, MetricNames.BOUGHT_TTHREE_AXE }
        };

        if(!itemsMetricNames.ContainsKey(item))
            return;

        string metricName = itemsMetricNames[item];
        if(!progression.HasMetric(metricName)) 
            progression.AddMetric(new ProgressionMetric(metricName, ProgressionMetric.MetricType.Boolean, false));

        progression.GetMetric(metricName).SetValue(true);           
        pd.SetData(progression);
    }
}