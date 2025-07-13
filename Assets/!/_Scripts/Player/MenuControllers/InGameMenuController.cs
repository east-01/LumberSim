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
    public SelectableRenderer SelectableRenderer => grabbableRenderer;
    [SerializeField]
    private AxeCriticalBarController axeCriticalBarController;
    public AxeCriticalBarController AxeCriticalBarController => axeCriticalBarController;
    [SerializeField]
    private HotbarController hotbarController;
    public HotbarController HotbarController => hotbarController;

    [Header("References - UI")]
    [SerializeField]
    private TMP_Text toolbeltText;

    protected override void Opened()
    {
        base.Opened();

        player.SetPaused(false);
        player.ConsumeMouse(true);
    }

    public override void SendMenuBack()
    {
        // base.SendMenuBack();
    }
    
}