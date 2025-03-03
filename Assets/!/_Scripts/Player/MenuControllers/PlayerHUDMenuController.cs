using System.Collections;
using System.Collections.Generic;
using EMullen.MenuController;
using EMullen.PlayerMgmt;
using TMPro;
using UnityEngine;

public class PlayerHUDMenuController : MenuController
{
    [SerializeField]
    private TMP_Text balanceText;

    private Player player;

    protected new void Awake()
    {
        base.Awake();
    
        player = GetComponentInParent<Player>();
        if(player == null) {
            Debug.LogError("Failed to get player in parent. It is assumed that the PlayerHUDMenuController is on a canvas that's a child of a Player GameObject.");
            return;
        }
    }

    private void Update()
    {
        if(player == null && player.uid.Value != null)
            return;
        
        string uid = player.uid.Value;
        PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(uid);
        GeneralPlayerData gpd;
        if(!pd.HasData<GeneralPlayerData>()) {
            gpd = new(50);
            pd.SetData(gpd);
        } else
            gpd = pd.GetData<GeneralPlayerData>();

        balanceText.text = "$" + gpd.balance.ToString("F2");
    }
}
