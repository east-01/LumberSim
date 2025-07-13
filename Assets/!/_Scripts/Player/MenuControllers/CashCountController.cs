using EMullen.Core;
using EMullen.PlayerMgmt;
using TMPro;
using UnityEngine;

/// <summary>
/// The cash count controller observes the players balance, if it changes it the controller will
///   automatically represent the change.
/// </summary>
public class CashCountController : MonoBehaviour 
{

    [Header("References")]
    [SerializeField]
    private Player player;
    [SerializeField]
    private TMP_Text balanceText;
    [SerializeField]
    private TMP_Text diffText;
    
    [Header("Settings")]
    [SerializeField]
    private float accumulateTimeout = 5f;
    [SerializeField]
    private float adjustTime = 0.5f;
    [SerializeField]
    private Color positiveFlowColor;
    [SerializeField]
    private Color negativeFlowColor;

    // Variables
    /// <summary>
    /// The balance that we're showing, not necessarily equal to player balance since the display
    ///   smoothly interpolates to the target balance.
    /// </summary>
    private float shownBalance;
    private float lastSeenBalance;
    private float lastBalanceChangeTime;
    private float lastAccumulatedTime;
    private float accumulatedBalance;

    public float TimeSinceLastBalanceChange => Time.time - lastBalanceChangeTime;
    public bool IsAccumulating => lastAccumulatedTime != -1;

    private void Update()
    {
        if(player == null && player.uid.Value != null)
            return;
        
        string uid = player.uid.Value;
        PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(uid);
        pd.EnsureLumberData();
        GeneralPlayerData gpd = pd.GetData<GeneralPlayerData>();

        if(lastSeenBalance != gpd.balance) {
            StartAccumulating();
            lastBalanceChangeTime = Time.time;
        }

        if(shownBalance != gpd.balance) {
            float adjustProgress = Mathf.Clamp01(TimeSinceLastBalanceChange/adjustTime);
            shownBalance = Mathf.Lerp(shownBalance, gpd.balance, adjustProgress);

            balanceText.text = "$" + shownBalance.ToString("F2");            
        }

        CheckAccumulationTimeout();
        UpdateAccumulation(gpd.balance);

        lastSeenBalance = gpd.balance;

    }

    private void UpdateAccumulation(float balance) 
    {
        // Cash flash
        if(balance != lastSeenBalance && IsAccumulating) { // Detect cash changes
            accumulatedBalance += balance - lastSeenBalance;
            lastAccumulatedTime = Time.time;
        }

        if(diffText == null)
            return;

        bool isPositiveCashFlow = Mathf.Sign(accumulatedBalance) != -1;
        string prefix = isPositiveCashFlow ? "+" : "-";
        string cashAmt = Mathf.Abs(accumulatedBalance).ToString("F2");
        diffText.text = $"{prefix}${cashAmt}";
        diffText.color = isPositiveCashFlow ? positiveFlowColor : negativeFlowColor;
    }

    private void CheckAccumulationTimeout() 
    {
        if(IsAccumulating && TimeSinceLastBalanceChange > accumulateTimeout)
            StopAccumulating();
    }

    public void StartAccumulating() 
    {
        if(diffText != null)
            diffText.gameObject.SetActive(true);

        lastAccumulatedTime = Time.time;
        accumulatedBalance = 0;
    }

    public void StopAccumulating() 
    {
        if(lastAccumulatedTime == -1)
            return;

        if(diffText != null)
            diffText.gameObject.SetActive(false);
        accumulatedBalance = 0;
        lastAccumulatedTime = -1f;
    }

}