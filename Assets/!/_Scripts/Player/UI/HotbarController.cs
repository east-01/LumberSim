using System;
using System.Collections;
using System.Collections.Generic;
using EMullen.Core;
using EMullen.PlayerMgmt;
using UnityEngine;
using UnityEngine.UIElements;

public class HotbarController : MonoBehaviour
{

    [SerializeField]
    private ItemMeshRenderer[] itemRenderers;

    [SerializeField]
    private float highlightedAlpha = 0.8f;
    [SerializeField]
    private float normalAlpha = 0.35f;

    private Player player;
    private ToolBelt toolBelt;

    private int selectedIndex = 0;

    private void Awake()
    {
        player = GetComponentInParent<Player>();
        if(player == null) {
            Debug.LogError("Failed to get player in parent. It is assumed that the PlayerHUDMenuController is on a canvas that's a child of a Player GameObject.");
            return;
        }
        toolBelt = player.GetComponent<ToolBelt>();
        if(toolBelt == null) {
            Debug.LogError("Failed to get ToolBelt on Player component, it is assumed that the ToolBelt component is on the same GameObject as the Player.");
            return;
        }        

        PlayerDataRegistry.Instance.PlayerDataUpdatedEvent += PlayerDataRegistry_PlayerDataUpdatedEvent;
    }

    private void Start()
    {
        UpdateToolbelt();

    }

    private void Update()
    {
        // TODO: Temp solution. The hotbar isn't updated once the player loads
        if(Mathf.FloorToInt(Time.time) != Mathf.FloorToInt(Time.time - Time.deltaTime))
            UpdateToolbelt();

        int idx = toolBelt.ToolbeltIndex;
        if(selectedIndex != idx) {
            selectedIndex = idx;
            UpdateToolbelt();
        }
    }

    private void UpdateToolbelt() 
    {
        for(int i = 0; i < itemRenderers.Length; i++) {
            UnityEngine.UI.Image img = itemRenderers[i].GetComponentInParent<UnityEngine.UI.Image>();
            Color col = img.color;
            col.a = i == selectedIndex ? highlightedAlpha : normalAlpha; 
            img.color = col;
        }   

        PlayerData playerData = player.PlayerData;
        playerData.EnsureLumberData();

        InventoryData data = playerData.GetData<InventoryData>();

        if(data.hotbarItems.Length != itemRenderers.Length)
            Debug.LogError("Hot bar items in InventoryData length is different from the amount of itemRenderers in HotbarController.");
    
        for(int i = 0; i < Math.Min(data.hotbarItems.Length, itemRenderers.Length); i++) {
            itemRenderers[i].ShowItemMesh(data.hotbarItems[i]);

            SetLayerRecursively(itemRenderers[i].CurrentItemMesh, LayerMask.NameToLayer("UI"));    
        }
    }

    private void PlayerDataRegistry_PlayerDataUpdatedEvent(PlayerData playerData, PlayerDataClass newData)
    {
        if(newData.GetType() != typeof(InventoryData))
            return;

        UpdateToolbelt();
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;

        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            if (child != null)
            {
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }
    }

}
