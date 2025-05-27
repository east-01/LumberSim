using System;
using System.Collections.Generic;
using EMullen.Core;
using EMullen.PlayerMgmt;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Player))]
[RequireComponent(typeof(NetworkedAudioController))]
public class ToolBelt : NetworkBehaviour, IInputListener
{
    [Header("List of events passed to ToolBeltImpl classes")]
    [SerializeField]
    private List<string> passedThroughEvents = new() {"Primary"};
    [SerializeField]
    private List<ItemImplementations> implementations;
    private Dictionary<Item, ToolBeltImpl> implementationsSorted;

    private Player player;
    private ItemMeshRenderer itemRenderer;

    public int ToolbeltIndex { get; private set; } = 0;    

    private void Awake()
    {
        player = GetComponent<Player>();
        itemRenderer = player.GetComponentInChildren<ItemMeshRenderer>(true);
        if(itemRenderer == null) {
            Debug.LogError("Failed to get ItemMeshRenderer on child of Player component, it is assumed that the ItemMeshRenderer component is a child of the Player.");
            return;
        }        

        implementationsSorted = new();
        foreach(ItemImplementations impls in implementations) {
            foreach(Item item in impls.items) {
                if(implementationsSorted.ContainsKey(item)) {
                    Debug.LogError($"Item implementation for \"{item}\" has already been set as {implementationsSorted[item]} but its set to target {impls.implementation} as well.");
                    continue;
                }
                implementationsSorted.Add(item, impls.implementation);
            }
        }
    }

    public void InputEvent(InputAction.CallbackContext context)
    {
        PlayerData pd = player.PlayerData;
        if(pd == null) {
            Debug.LogError("Can't execute InputEvent player doesn't have PlayerData.");
            return;
        }
        InventoryData id = pd.GetData<InventoryData>();

        Item GetCurrentItem() => id.hotbarItems[ToolbeltIndex];

        if(context.action.name == "ChangeToolbelt") {

            if(!context.performed)
                return;

            ToolbeltIndex = (ToolbeltIndex+1)%id.hotbarItems.Length;

            UpdateRenderer();
        } else if(passedThroughEvents.Contains(context.action.name)){

            Item item = GetCurrentItem();

            if(!implementationsSorted.ContainsKey(item)) {
                Debug.LogError($"Can't execute InputEvent there is no implementation set for \"{item}\"");
                return;
            }

            implementationsSorted[item].HandleInput(item, context);
        }
    }

    public void UpdateRenderer() 
    {
        PlayerData pd = player.PlayerData;
        if(pd == null) {
            Debug.LogError("Can't execute UpdateRenderer() player doesn't have PlayerData.");
            return;
        }
        InventoryData id = pd.GetData<InventoryData>();

        try {
            itemRenderer.ShowItemMesh(id.hotbarItems[ToolbeltIndex], false);
        } catch(InvalidOperationException) {
            itemRenderer.ShowItemMesh(Item.NONE, false);
        }
    }

    // InputPolling is toggled off
    public void InputPoll(InputAction action) {}

    [Serializable]
    private struct ItemImplementations 
    {
        public List<Item> items;
        public ToolBeltImpl implementation;
    }
}

/// <summary>
/// The ToolBeltImplementation class holds concrete implementations for different item types.
/// </summary>
public abstract class ToolBeltImpl : NetworkBehaviour
{
    public abstract void HandleInput(Item item, InputAction.CallbackContext context);
}
