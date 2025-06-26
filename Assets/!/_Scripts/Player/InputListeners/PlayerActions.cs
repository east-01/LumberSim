using EMullen.Core;
using EMullen.PlayerMgmt;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The player actions input listener class is a catch all for all extra player actions that don't
///   group well. Currently holds the axe upgrade action.
/// </summary>
[RequireComponent(typeof(Player))]
[RequireComponent(typeof(ToolBelt))]
[RequireComponent(typeof(VehicleDriver))]
[RequireComponent(typeof(NetworkedAudioController))]
public class PlayerActions : NetworkBehaviour, IInputListener
{

    [SerializeField]
    private GameObject grabbableItemPrefab; // For item drops
    [SerializeField]
    private ItemAssignments itemAssignments;

    private Player player;
    private ToolBelt toolBelt;
    private VehicleDriver vehicleDriver;
    private NetworkedAudioController audioController;

    private void Awake()
    {
        player = GetComponent<Player>();
        toolBelt = GetComponent<ToolBelt>();
        vehicleDriver = GetComponent<VehicleDriver>();
        audioController = GetComponent<NetworkedAudioController>();
    }

    public void InputEvent(InputAction.CallbackContext context)
    {
        PlayerData pd = player.PlayerData;
        pd.EnsureLumberData();

        switch(context.action.name) {
            case "Interact":
                if(context.performed && vehicleDriver.isDriving) {
                    vehicleDriver.ClearDrivingVehicle();
                    break;
                }
    
                HandleInteract(context);
                break;       
            case "DropHotbar":
                HandleDropHotbar(context);
                break;       
        }
    }

    private void HandleInteract(InputAction.CallbackContext context) 
    {
        if(!context.performed)
            return;

        Grabbable grabbable = player.GrabbablePicker.PickGrabbable(5f);
        if(grabbable == null)
            return;

        if(grabbable.TryGetComponent(out GrabbableItem grabbableItem))
            HandleInteractWithGrabbableItem(grabbableItem);
        else if(grabbable.TryGetComponent(out Vehicle vehicle))
            HandleInteractWithVehicle(vehicle);
    }

    private void HandleInteractWithGrabbableItem(GrabbableItem grabbableItem) 
    {
        Item item = grabbableItem.item.Value;
        if(item == Item.NONE)
            return;

        if(grabbableItem.Owner.IsValid && grabbableItem.Owner != InstanceFinder.ClientManager.Connection)
            return;

        PlayerData pd = player.PlayerData;
        pd.EnsureLumberData();

        ItemInfo info = itemAssignments.Get(item);

        if(!grabbableItem.Owner.IsValid && pd.GetData<GeneralPlayerData>().balance < info.cost) {
            player.GetHUD().ShowWarning($"Can't afford", 2f);
            return;
        }

        grabbableItem.GrabbedItem(InstanceFinder.ClientManager.Connection, player.uid.Value);
        toolBelt.UpdateRenderer();
    }

    private void HandleInteractWithVehicle(Vehicle vehicle) 
    {
        BLog.Highlight("Interacted");
        if(vehicle.HasDriver())
            return;

        vehicleDriver.SetDrivingVehicle(vehicle.NetworkObject);
    }

    private void HandleDropHotbar(InputAction.CallbackContext context) 
    {
        if(!context.performed)
            return;

        PlayerData pd = player.PlayerData;
        pd.EnsureLumberData();

        InventoryData id = pd.GetData<InventoryData>();
        Item[] hotbarItems = id.hotbarItems;
        int idx = toolBelt.ToolbeltIndex;

        if(hotbarItems[idx] == Item.NONE)
            return;

        SpawnDropItem(hotbarItems[idx]);        

        // Update player's inventory
        hotbarItems[idx] = Item.NONE;
        id.hotbarItems = hotbarItems;
        pd.SetData(id);

        toolBelt.UpdateRenderer();
    }

    /// <summary>
    /// Part of the hotbar drop process that involves actually spawning the dropped item
    /// </summary>
    private void SpawnDropItem(Item item, NetworkConnection owner = null) 
    {
        if(!InstanceFinder.IsServerStarted) {
            ServerRPCSpawnDropItem(item, InstanceFinder.ClientManager.Connection);
            return;
        }
        
        owner ??= LocalConnection;

        // Spawn the GrabbableItem
        Transform camTransform = player.Camera.transform;
        Vector3 initPosition = camTransform.position + camTransform.forward.normalized * 0.3f;
        Vector3 initVelocity = camTransform.forward.normalized * 5f;
        GameplayManager.SpawnGrabbaleArgs args = new(initPosition, camTransform.rotation, initVelocity, grabbableItemPrefab, owner);
        Grabbable grabbable = player.GameplayManager.SpawnGrabbable(args);

        GrabbableItem grabbableItem = grabbable.GetComponent<GrabbableItem>();

        grabbableItem.SetItem(item);
    }
    [ServerRpc(RequireOwnership =false)]
    private void ServerRPCSpawnDropItem(Item item, NetworkConnection owner) => SpawnDropItem(item, owner);

    public void InputPoll(InputAction action) {}

}
