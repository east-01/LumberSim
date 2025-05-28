using EMullen.Core;
using UnityEngine;
using UnityEngine.InputSystem;

public class AxeImpl : ToolBeltImpl 
{
    [SerializeField]
    private ItemAssignments itemAssignments;

    // Timestamps marking the beginning/end of the axe swing
    private float axeSwingStart;
    private float axeSwingEnd;
    public float AxeSwingProgress => Mathf.Clamp01((Time.time-axeSwingStart)/(axeSwingEnd-axeSwingStart));

    private Player player;

    private void Awake()
    {
        player = GetComponentInParent<Player>();   
    }

    private void Start()
    {
        axeSwingStart = axeSwingEnd = float.NegativeInfinity;   
    }

    public override void HandleInput(Item item, InputAction.CallbackContext context)
    {
        ItemInfo itemInfo = itemAssignments.Get(item);
        if(itemInfo is not AxeInfo) {
            Debug.LogError($"Can't handle input in AxeImpl.cs, item info for {item} is not an instance of AxeInfo");
            return;
        }
        AxeInfo axeInfo = itemInfo as AxeInfo;

        if(context.action.name == "Primary" && context.performed) {
            Primary(axeInfo.hitPower, axeInfo.rechargeTime);
        }
    }

    /// <summary>
    /// Swing the axe tool.
    /// </summary>
    private void Primary(float hitPower, float rechargeTime) 
    {
        // Ensure axe is ready
        if(AxeSwingProgress < 1)
            return;

        player.GetNetworkedAudioController().PlaySound("swingaxe");

        // Try to pick a log, if we miss return
        LogPickArgs args = PickLog();
        if(args == null)
            return;

        axeSwingStart = Time.time;
        axeSwingEnd = Time.time + rechargeTime;

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