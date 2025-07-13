using EMullen.Core;
using EMullen.MenuController;
using EMullen.PlayerMgmt;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
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
            case "ToggleMenu":
                HandleToggleMenu(context);
                break;
            case "Pause":
                HandlePauseMenu(context);
                break;
        }
    }

    private void HandleInteract(InputAction.CallbackContext context) 
    {
        if(!context.performed)
            return;

        Selectable grabbable = player.RaycastPicker.PickSelectable(5f);
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

        PlayerData pd = player.PlayerData;
        pd.EnsureLumberData();

        if(grabbableItem.Owner.IsValid) {
            if(grabbableItem.IsOwner) {
                grabbableItem.GrabbedItem(InstanceFinder.ClientManager.Connection, player.uid.Value);
            } else {
                player.ShowHUDWarning($"You're not the owner of this item.", 3f);
            }
        } else {
            player.PlayerTransactionManager.PurchaseGrabbableItem(grabbableItem.NetworkObject);
        }

        toolBelt.UpdateRenderer();
    }

    private void HandleInteractWithVehicle(Vehicle vehicle) 
    {
        if(vehicle.HasDriver())
            return;

        if(!vehicle.IsOwner) {
            // player.ShowHUDWarning("You don't own the vehicle.", 2f);
            // return;
            ClaimVehicle(vehicle.NetworkObject, LocalConnection);    
            player.ShowHUDWarning("TEMP: Claimed vehicle", 2f);
            return;
        }

        BLog.Highlight($"veh owner: {vehicle.Owner}");

        vehicleDriver.SetDrivingVehicle(vehicle.NetworkObject);
    }

    public void ClaimVehicle(NetworkObject nob, NetworkConnection client) {
        if(!InstanceFinder.IsServerStarted) {
            ServerRPCClaimVehicle(nob, LocalConnection);
            return;
        }

        nob.GiveOwnership(client);
    }
    [ServerRpc(RequireOwnership = false)]
    public void ServerRPCClaimVehicle(NetworkObject nob, NetworkConnection client) => ClaimVehicle(nob, client);

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

    private void HandleToggleMenu(InputAction.CallbackContext context) 
    {
        if(!context.performed)
            return; 

        PlayerHUDMenuController hud = player.PlayerHUD;
        MenuController progSubmenu = hud.GetSubMenu(PlayerHUDMenuController.SUBMENU_PROGRESSION);
        MenuController igSubmenu = hud.GetSubMenu(PlayerHUDMenuController.SUBMENU_IN_GAME);
        if(progSubmenu.IsOpen) {
            progSubmenu.Close();
            igSubmenu.Open();
        } else {
            igSubmenu.Close();
            progSubmenu.Open();
        }
    }

    private void HandlePauseMenu(InputAction.CallbackContext context) 
    {
        if(!context.performed)
            return; 

        PlayerHUDMenuController hud = player.PlayerHUD;
        MenuController pauseSubmenu = hud.GetSubMenu(PlayerHUDMenuController.SUBMENU_PAUSE);
        MenuController igSubmenu = hud.GetSubMenu(PlayerHUDMenuController.SUBMENU_IN_GAME);
        if(pauseSubmenu.IsOpen) {
            pauseSubmenu.Close();
            igSubmenu.Open();
        } else {
            igSubmenu.Close();
            pauseSubmenu.Open();
        }
    }

    public void InputPoll(InputAction action) {}

}
