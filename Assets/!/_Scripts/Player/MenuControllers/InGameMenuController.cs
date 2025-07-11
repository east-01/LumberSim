using EMullen.Core;
using EMullen.MenuController;
using EMullen.PlayerMgmt;
using TMPro;
using UnityEngine;

public class InGameMenuController : MenuController 
{
    [Header("References")]
    [SerializeField]
    private Player player;
    [SerializeField]
    private SelectableRenderer grabbableRenderer;
    public SelectableRenderer GrabbableRenderer => grabbableRenderer;
    [SerializeField]
    private AxeCriticalBarController axeCriticalBarController;
    public AxeCriticalBarController AxeCriticalBarController => axeCriticalBarController;
    [SerializeField]
    private HotbarController hotbarController;
    public HotbarController HotbarController => hotbarController;

    [Header("References - UI")]
    [SerializeField]
    private TMP_Text toolbeltText;
    [SerializeField]
    private TMP_Text balanceText;
    [SerializeField]
    private TMP_Text cashFlashText;

    [Header("Settings")]
    [SerializeField]
    private float cashFlashTimeout;
    [SerializeField]
    private Color positiveCashFlowColor;
    [SerializeField]
    private Color negativeCashFlowColor;

    private float cfLastBalance;
    private float cfLastAccumulatedTime;
    private float cfAccumulatedBalance;

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

        // ----- Frame-change trackers -----
        cfLastBalance = gpd.balance;

    }

    protected override void Opened()
    {
        base.Opened();
        BLog.Highlight("Opened");

        player.SetPaused(false);
        player.ConsumeMouse(true);
    }

    public override void SendMenuBack()
    {
        base.SendMenuBack();
        BLog.Highlight("Sending menu back");
    }
    
}