using System.Collections;
using System.Collections.Generic;
using EMullen.MenuController;
using EMullen.PlayerMgmt;
using TMPro;
using UnityEngine;

/// <summary>
/// The PlayerHUDMenuController class is responsible for controlling everything in the player's
///   pov, shows warning messages, balance, and tool belt.
/// </summary>
public class PlayerHUDMenuController : MenuController
{

    public static readonly string SUBMENU_IN_GAME = "InGame";
    public static readonly string SUBMENU_PROGRESSION = "Progression";

    [Header("References")]
    [SerializeField]
    private GrabbableRenderer grabbableRenderer;
    public GrabbableRenderer GrabbableRenderer => grabbableRenderer;

    [SerializeField]
    private AxeCriticalBarController axeCriticalBarController;
    public AxeCriticalBarController AxeCriticalBarController => axeCriticalBarController;

    [SerializeField]
    private TMP_Text toolbeltText;
    [SerializeField]
    private TMP_Text toolbeltTextSecondary;

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

    [Header("Settings")]
    [SerializeField]
    private Color positiveCashFlowColor;
    [SerializeField]
    private Color negativeCashFlowColor;

    private Player player;
    private ToolBelt toolBelt;

    protected new void Awake()
    {
        base.Awake();
    
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
    }

    private void Update()
    {
        if(player == null && player.uid.Value != null)
            return;
        
        // ----- Load player info -----
        string uid = player.uid.Value;
        PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(uid);
        pd.EnsureLumberData();
        GeneralPlayerData gpd = pd.GetData<GeneralPlayerData>();

        // ----- Display balance text -----
        balanceText.text = "$" + gpd.balance.ToString("F2");

        // Cash flash
        if(gpd.balance != cfLastBalance) { // Detect cash changes
            cfAccumulatedBalance += gpd.balance - cfLastBalance;
            cfLastAccumulatedTime = Time.time;
        }

        bool isCashFlashActive = Time.time - cfLastAccumulatedTime <= cashFlashTimeout;
        cashFlashText.gameObject.SetActive(isCashFlashActive);
        if(isCashFlashActive) {
            bool isPositiveCashFlow = Mathf.Sign(cfAccumulatedBalance) != -1;
            string prefix = isPositiveCashFlow ? "+" : "-";
            string cashAmt = Mathf.Abs(cfAccumulatedBalance).ToString("F2");
            cashFlashText.text = $"{prefix}${cashAmt}";
            cashFlashText.color = isPositiveCashFlow ? positiveCashFlowColor : negativeCashFlowColor;
        } else
            cfAccumulatedBalance = 0;

        // ----- Warning text -----
        warningText.gameObject.SetActive(Time.time < warningHideTime);

        // ----- Frame-change trackers -----
        cfLastBalance = gpd.balance;

    }

    /// <summary>
    /// Flash a warning message on the screen for a specific amount of time.
    /// </summary>
    /// <param name="message">The warning message to show.</param>
    /// <param name="time">The time in seconds for the message to persist.</param>
    public void ShowWarning(string message, float time) 
    {
        warningText.text = message;
        warningText.gameObject.SetActive(true);
        warningHideTime = Time.time+time;
    }
}
