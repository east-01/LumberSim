using System;
using EMullen.Core;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;

public class AxeImpl : ToolBeltImpl 
{
    [Header("References")]
    [SerializeField]
    private ItemAssignments itemAssignments;
    [Header("Settings")]
    [SerializeField]
    private AnimationCurve swingSpeedCurve;
    [SerializeField]
    private float defaultHitPowerMultiplier = 0.5f;
    /// <summary>
    /// What % of swing (in [0,1] range) will yield a critical hit.
    /// If the value is 0.05 (5%), then precise multipliers of 0.95 or better are critical.
    /// </summary>
    [SerializeField]
    private float criticalWindowSize = 0.5f;

    // Timestamps marking the beginning/end of the axe swing
    private float axeSwingStart;
    private float axeSwingEnd;
    private bool isPrecise;
    public float AxeSwingProgress;
    public bool AxeSwingActive { get; private set; }

    private Player player;
    private AxeCriticalBarController axeCriticalBarController => (player.PlayerHUD.GetSubMenu(PlayerHUDMenuController.SUBMENU_IN_GAME) as InGameMenuController).AxeCriticalBarController;

    private AxePhase phase;
    public AxePhase Phase {
        get => phase;
        set {
            phaseSetTime = Time.time;
            phase = value;
        }
    }
    private float phaseSetTime;
    public float TimeInPhase => Time.time - phaseSetTime;
    private float rechargeEndTime;

    private float targetCriticalValue;

    private void Awake()
    {
        player = GetComponentInParent<Player>();   
    }

    private void Start()
    {
        axeSwingStart = axeSwingEnd = float.NegativeInfinity;   
        Phase = AxePhase.READY;
    }

    private void Update()
    {
        float raw01Progress = Mathf.Clamp01((Time.time-axeSwingStart)/(axeSwingEnd-axeSwingStart));
        AxeSwingProgress = swingSpeedCurve.Evaluate(raw01Progress);
        AxeSwingActive = Time.time >= axeSwingStart && Time.time <= axeSwingEnd;

        AxeCriticalBarController axeController = axeCriticalBarController;

        if(Phase == AxePhase.SWINGING) {
            axeController.cursorValue = AxeSwingProgress;
            axeController.targetValue = targetCriticalValue;

            if(Time.time > axeSwingEnd) {
                Phase = AxePhase.COOLDOWN;
            
                axeCriticalBarController.PlayMiss();
            }
        } else if(Phase == AxePhase.COOLDOWN) {
            if(TimeInPhase >= rechargeEndTime)
                Phase = AxePhase.READY;
        } 

    }

    public override void HandleInput(Item item, InputAction.CallbackContext context)
    {
        if(!enabled)
            return;

        ItemInfo itemInfo = itemAssignments.Get(item);
        if(itemInfo is not AxeInfo) {
            Debug.LogError($"Can't handle input in AxeImpl.cs, item info for {item} is not an instance of AxeInfo");
            return;
        }
        AxeInfo axeInfo = itemInfo as AxeInfo;

        if(context.action.name == "Primary" && context.performed) {
            Primary(axeInfo);
        } else if(context.action.name == "Secondary" && context.performed) {
            Secondary(axeInfo);
        }
    }

    /// <summary>
    /// Swing the axe tool.
    /// </summary>
    private void Primary(AxeInfo axeInfo) 
    {
        switch(Phase) 
        {
            case AxePhase.READY:
        
                isPrecise = false;
                HandleSwing(axeInfo);   

                rechargeEndTime = axeInfo.rechargeTime;
                
                player.NetworkedAudioController.PlaySound("swingaxe");
                
                Phase = AxePhase.COOLDOWN;
                break;
        }
    }

    private void Secondary(AxeInfo axeInfo) 
    {
        // BLog.Highlight($"Swung with axe progess: {AxeSwingProgress}");
        // BLog.Highlight($"Swung in state: {Phase}");

        switch (Phase)
        {
            case AxePhase.READY:
                axeSwingStart = Time.time;
                axeSwingEnd = Time.time + axeInfo.swingTime;

                targetCriticalValue = UnityEngine.Random.Range(0.55f, 0.8f);

                Phase = AxePhase.SWINGING;
                // BLog.Highlight($"Phase is now {Phase}");s
                break;

            case AxePhase.SWINGING:
                isPrecise = true;
                HandleSwing(axeInfo);

                rechargeEndTime = axeInfo.rechargeTime;

                player.NetworkedAudioController.PlaySound("swingaxe");
        
                Phase = AxePhase.COOLDOWN;
                break;

            case AxePhase.COOLDOWN:
                
                break;
        }

    }

    private void HandleSwing(AxeInfo axeInfo) 
    {
        // Try to pick a log, if we miss return
        LogPickArgs args = PickLog();
        if(args == null)
            return;

        float hitPower = axeInfo.hitPower;
        float preciseMultiplier = 1 - Mathf.Clamp01(Mathf.Abs(targetCriticalValue - AxeSwingProgress));
        if(preciseMultiplier >= 1 - criticalWindowSize)
            preciseMultiplier = 1.25f;
        float powerMultiplier = isPrecise ? preciseMultiplier : defaultHitPowerMultiplier;

        hitPower *= powerMultiplier;

        if(powerMultiplier == 1.25f)
            axeCriticalBarController.PlayCriticalHit();
        else if(powerMultiplier >= 0.7f)
            axeCriticalBarController.PlayMediumHit();
        else 
            axeCriticalBarController.PlaySmallHit();

        // BLog.Highlight("hitPower multiplier: " + powerMultiplier + " TODO: Make this a particle effect");

        int[] identifierPath = args.log.GetIdentifierPath();
        TreeLogGroup.SingleHitData hitData = new TreeLogGroup.SingleHitData(identifierPath, args.hit.point, hitPower, LocalConnection);
        args.group.HitLog(hitData);
    }

    /// <summary>
    /// Given the player's camera pick a Log object
    /// </summary>
    /// <returns>null if the Raycast hit nothing or a non log GameObject, LogPickArgs if hit. </returns>
    private LogPickArgs PickLog() 
    {
        RaycastHit hit;
        Physics.Raycast(player.Camera.transform.position, player.Camera.transform.forward, out hit, 10f);
        if(hit.collider == null)
            return null;

        Vector3 hitPointLocal = hit.transform.InverseTransformPoint(hit.point);
        // We do this because we know that the TreeLogVisuals are a child of the TreeLog GameObject

        if(hit.collider == null || hit.collider.transform.parent == null)
            return null;

        TreeLog log = hit.collider.transform.parent.gameObject.GetComponent<TreeLog>();

        if(log == null)
            return null;

        TreeLogGroup group = log.GetComponentInParent<TreeLogGroup>();

        if(group == null) {
            Debug.LogError($"Failed to find TreeLogGroup component in parent of game object \"{log.gameObject.name}\"");
            return null;
        }

        return new(hit, hitPointLocal, log, group);
    }

    public enum AxePhase { READY, SWINGING, COOLDOWN }

    /// <summary>
    /// Class for when the user clicks on a log.
    /// </summary>
    private class LogPickArgs {
        public RaycastHit hit;
        public Vector3 hitPointLocal;
        public TreeLog log;
        public TreeLogGroup group;

        public LogPickArgs(RaycastHit hit, Vector3 hitPointLocal, TreeLog log, TreeLogGroup group)
        {
            this.hit = hit;
            this.hitPointLocal = hitPointLocal;
            this.log = log;
            this.group = group;
        }
    }
}