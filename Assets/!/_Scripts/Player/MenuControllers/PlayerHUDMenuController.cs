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
    [SerializeField]
    private TMP_Text cashFlashText;
    [SerializeField]
    private float cashFlashTimeout;
    private float cfLastBalance;
    private float cfLastAccumulatedTime;
    private float cfAccumulatedBalance;

    [SerializeField]
    private TMP_Text warningText;
    private float warningHideTime;

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
            gpd = new(20);
            pd.SetData(gpd);
        } else
            gpd = pd.GetData<GeneralPlayerData>();

        // Display balance text
        balanceText.text = "$" + gpd.balance.ToString("F2");

        // Cash flash
        if(gpd.balance > cfLastBalance) { // Detect cash increase
            cfAccumulatedBalance += gpd.balance - cfLastBalance;
            cfLastAccumulatedTime = Time.time;
        }

        cashFlashText.gameObject.SetActive(Time.time - cfLastAccumulatedTime <= cashFlashTimeout);
        if(cashFlashText.gameObject.activeSelf)
            cashFlashText.text = $"+${cfAccumulatedBalance.ToString("F2")}";
        else
            cfAccumulatedBalance = 0;

        // Warning text
        warningText.gameObject.SetActive(Time.time < warningHideTime);

        // Frame-change trackers
        cfLastBalance = gpd.balance;

    }

    public void ShowWarning(string message, float time) 
    {
        warningText.text = message;
        warningText.gameObject.SetActive(true);
        warningHideTime = Time.time+time;
    }
}
