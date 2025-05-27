using EMullen.Core;
using EMullen.PlayerMgmt;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The player actions input listener class is a catch all for all extra player actions that don't
///   group well. Currently holds the axe upgrade action.
/// </summary>
[RequireComponent(typeof(Player))]
[RequireComponent(typeof(ToolBelt))]
[RequireComponent(typeof(NetworkedAudioController))]
public class PlayerActions : MonoBehaviour, IInputListener
{

    [SerializeField]
    private GameObject grabbableItemPrefab; // For item drops

    private Player player;
    private ToolBelt toolBelt;
    private NetworkedAudioController audioController;

    private void Awake()
    {
        player = GetComponent<Player>();
        toolBelt = GetComponent<ToolBelt>();
        audioController = GetComponent<NetworkedAudioController>();
    }

    public void InputEvent(InputAction.CallbackContext context)
    {
        PlayerData pd = player.PlayerData;
        pd.EnsureLumberData();

        switch(context.action.name) {
            case "Interact":
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

        if(!grabbable.TryGetComponent(out GrabbableItem grabbableItem))
            return;

        Item item = grabbableItem.item.Value;

        if(item == Item.NONE)
            return;

        // Add item to hotbar, if not successful break early
        PlayerData pd = player.PlayerData;
        pd.EnsureLumberData();

        InventoryData inventoryData = pd.GetData<InventoryData>();
        if(!inventoryData.AddItemToHotbar(item))
            return;

        pd.SetData(inventoryData);

        grabbableItem.GrabbedItem();
        toolBelt.UpdateRenderer();
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

        // Spawn the GrabbableItem
        Transform camTransform = player.Camera.transform;
        Vector3 initPosition = camTransform.position + camTransform.forward.normalized * 0.3f;
        Vector3 initVelocity = camTransform.forward.normalized * 5f;
        GameplayManager.SpawnGrabbaleArgs args = new(initPosition, camTransform.rotation, initVelocity, grabbableItemPrefab);
        Grabbable grabbable = player.GameplayManager.SpawnGrabbable(args);

        GrabbableItem grabbableItem = grabbable.GetComponent<GrabbableItem>();

        grabbableItem.SetItem(hotbarItems[idx]);

        // Update player's inventory
        hotbarItems[idx] = Item.NONE;
        id.hotbarItems = hotbarItems;
        pd.SetData(id);

        toolBelt.UpdateRenderer();
    }

    public void InputPoll(InputAction action) {}

}
