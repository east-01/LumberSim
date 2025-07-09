using System;
using System.Collections;
using System.Collections.Generic;
using EMullen.Core;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;

public class Grabbable : NetworkBehaviour, IS3
{
    [SerializeField]
    private Color defaultSelectColor;
    [SerializeField]
    private Color defaultNormalColor;

    private Outline _selectOutline;
    public Outline SelectOutline {
        get {
            _selectOutline ??= CreateOutline();
            return _selectOutline;
        }
        private set => _selectOutline = value;
    }

    [SerializeField]
    private GrabbableInfo info;
    public GrabbableInfo Info { get {
        IGrabbable grabbable = GetIGrabbable();
        if(grabbable != null && grabbable.OverrideGrabbableInfo() != null)
            return grabbable.OverrideGrabbableInfo();
        
        return info;
    } }
    [SerializeField]
    private bool zeroGravity = true;
    [SerializeField]
    private bool allowRotation = true;
    [SerializeField] 
    private float velocitySmoothing = 0.1f;
    [SerializeField]
    private float maxPercievedVelocity = 10f;
    [SerializeField] 
    private float rotationAccel = 720f;    // degrees per second²

    private Rigidbody rb;
    private GameplayManager gameplayManager;

    private Vector3 lastPos;
    public Vector3 PercievedVelocity { get; private set; }
    public Vector3 SmoothedPerceivedVelocity { get; private set; }
    private Vector3 localGrabPointLocal;
    private float currentRotationSpeed;  

    private bool isGrabbed;
    private NetworkConnection grabber;
    private string grabberUID;

    private void Awake() 
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        UpdateOutline();
        isGrabbed = false;
    }

    public void SingletonRegistered(Type type, object singleton)
    {
        if(type != typeof(GameplayManager))
            return;

        gameplayManager = singleton as GameplayManager;
    }

    public void SingletonDeregistered(Type type, object singleton)
    {
        if(type != typeof(GameplayManager))
            return;

    }

    private void Update()
    {
        // Safely subscribe to the GameplayManager singleton
        if(gameObject.scene.name == "GameplayScene") {
            SceneLookupData lookupData = gameObject.scene.GetSceneLookupData();

            if(!SceneSingletons.IsSubscribed(this, lookupData, typeof(GameplayManager))) {
                SceneSingletons.SubscribeToSingleton(this, lookupData, typeof(GameplayManager));
            }
        }
    }

    private void FixedUpdate()
    {
        var rawVelocity = (transform.position - lastPos) / Time.fixedDeltaTime;
        PercievedVelocity = rawVelocity;

        SmoothedPerceivedVelocity = Vector3.Lerp(SmoothedPerceivedVelocity, rawVelocity, velocitySmoothing);
        if(SmoothedPerceivedVelocity.magnitude > maxPercievedVelocity)
            SmoothedPerceivedVelocity = SmoothedPerceivedVelocity.normalized * maxPercievedVelocity;

        lastPos = transform.position;
    }

    public IGrabbable GetIGrabbable() 
    {
        if(gameObject.TryGetComponent(out IGrabbable onObjGrab))
            return onObjGrab;
        if(gameObject.transform.parent != null && gameObject.transform.parent.gameObject.TryGetComponent(out IGrabbable onParentGrab))
            return onParentGrab;

        Debug.LogWarning($"Can't get Grabbable on game object \"{gameObject.name}\"");
        return null;
    }

#region Grabbing/Movement
    public virtual void StartGrab(NetworkConnection grabber, string grabberUID, Vector3 hitPoint) 
    {
        IGrabbable grabbableInterface = GetIGrabbable();
        if(grabbableInterface != null && !grabbableInterface.CanPickup(grabber, grabberUID, out string reason)) {
            Player player = gameplayManager.PlayerObjectManager.GetPlayer(grabberUID);
            bool hasReason = reason != null && reason.Length > 0;
            player.ShowHUDWarning(hasReason ? reason : "Can't pickup", 2f);
            return;
        }

        this.grabber = grabber;
        this.grabberUID = grabberUID;
        localGrabPointLocal = transform.InverseTransformPoint(hitPoint);
        if(zeroGravity)
            rb.useGravity = false;

        if(!InstanceFinder.IsServerStarted) {
            ServerRPCStartGrab(grabber, grabberUID, hitPoint);
            return;
        }

        if(!Owner.IsValid)
            GiveOwnership(grabber);

        TargetRpcGrabStateChanged(grabber, grabberUID, NetworkObject);
    }
    [ServerRpc(RequireOwnership = false)]
    private void ServerRPCStartGrab(NetworkConnection grabber, string grabberUID, Vector3 hitPoint) => StartGrab(grabber, grabberUID, hitPoint);

    public virtual void StopGrab() 
    {
        NetworkConnection grabberBackup = this.grabber;
        string grabberUIDBackup = this.grabberUID;

        this.grabber = null;
        this.grabberUID = null;
        this.localGrabPointLocal = Vector3.zero;
        rb.useGravity = true;
        rb.velocity = SmoothedPerceivedVelocity;

        if(!InstanceFinder.IsServerStarted)
            ServerRPCStopGrab();
        else
            TargetRpcGrabStateChanged(grabberBackup, grabberUIDBackup, null);
    }
    [ServerRpc(RequireOwnership = false)]
    private void ServerRPCStopGrab() => StopGrab();

    public virtual void UpdatePositionAndRotation(Vector3 targetPosition, Quaternion rotationDelta)
    {
        if(grabber == null || !grabber.IsValid)
            return;

        Quaternion newRot = rb.rotation;
        if(allowRotation)
            newRot = UpdateRotation(rotationDelta);
        UpdatePosition(targetPosition, newRot);        

        if(!InstanceFinder.IsServerStarted)
            ServerRPCUpdatePositionAndRotation(targetPosition, rotationDelta);
    }
    [ServerRpc(RequireOwnership = false)]
    private void ServerRPCUpdatePositionAndRotation(Vector3 targetPosition, Quaternion rotationDelta) => UpdatePositionAndRotation(targetPosition, rotationDelta);

    private Quaternion UpdateRotation(Quaternion rotationDelta) 
    {
        Quaternion targetRot = rotationDelta * rb.rotation;
        Quaternion deltaQ = Quaternion.Inverse(rb.rotation) * targetRot;
        deltaQ.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f) 
            angle -= 360f;
        axis.Normalize();

        float desiredSpeed = angle / Time.fixedDeltaTime;    // deg/sec
        currentRotationSpeed = Mathf.MoveTowards(
            currentRotationSpeed,
            desiredSpeed,
            rotationAccel * Time.fixedDeltaTime
        );
        float stepAngle = currentRotationSpeed * Time.fixedDeltaTime;
        Quaternion stepQ = Quaternion.AngleAxis(stepAngle, axis);
        Quaternion newRot = stepQ * rb.rotation;
        rb.MoveRotation(newRot);

        return newRot;
    }

    private void UpdatePosition(Vector3 targetPosition, Quaternion newRot) 
    {
        Vector3 worldOffset = newRot * localGrabPointLocal;
        Vector3 desiredPos = targetPosition - worldOffset;

        Vector3 travel = desiredPos - rb.position;
        Vector3 remaining = travel;
        Vector3 movement = rb.position;
        float stepSize = 0.5f;        // half‐meter per mini‐step

        while (remaining.sqrMagnitude > 0.0001f)
        {
            Vector3 step = Vector3.ClampMagnitude(remaining, stepSize);
            if (CheckForHits(step, out Vector3 safe))
                movement += safe;
            else
                movement += step;

            remaining -= step;
        }

        Vector3 smoothedPos = Vector3.Lerp(rb.position, movement, 0.15f);
        rb.MovePosition(smoothedPos);
    }

    private bool CheckForHits(Vector3 vec, out Vector3 adjustedVec)
    {
        RaycastHit[] hits = rb.SweepTestAll(vec.normalized, vec.magnitude, QueryTriggerInteraction.Ignore);

        if (hits.Length == 0)
        {
            adjustedVec = vec;
            return false;
        }

        // sort so we handle nearest first
        if (hits.Length > 1)
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // project movement onto the collision planes
        adjustedVec = vec;
        foreach (var hit in hits)
            adjustedVec = Vector3.ProjectOnPlane(adjustedVec, hit.normal);

        return true;
    }

    // public void UpdatePositionAndRotation(Vector3 targetPosition, Quaternion rotationDelta) 
    // {
    //     if(Input.GetKeyDown(KeyCode.J))
    //         BLog.Highlight($"GrabOffset: {localOffset}");

    //     UpdateRotation(rotationDelta);
    //     UpdatePosition(targetPosition);

    //     if(!InstanceFinder.IsServerStarted)
    //         ServerRPCUpdatePositionAndRotation(targetPosition, rotationDelta);
    // }
    // [ServerRpc(RequireOwnership = false)]
    // private void ServerRPCUpdatePositionAndRotation(Vector3 targetPosition, Quaternion rotationDelta) => UpdatePositionAndRotation(targetPosition, rotationDelta);

    // private void UpdatePosition(Vector3 targetPosition) 
    // {
    //     Vector3 startPos = transform.position;
    //     float distToTarget = Vector3.Distance(transform.position, targetPosition);
    //     Vector3 newPos = Vector3.Lerp(startPos, targetPosition, Mathf.Max(distToTarget/10f, 0.05f));
    //     Vector3 travelVec = newPos-startPos;

    //     bool CheckForHits(Vector3 vec, out Vector3 adjustedVec) 
    //     {
    //         RaycastHit[] hits = rb.SweepTestAll(vec.normalized, vec.magnitude, QueryTriggerInteraction.Ignore);

    //         if(hits.Length == 0) {
    //             adjustedVec = vec;
    //             return false;
    //         }

    //         if (hits.Length > 1)
    //             Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

    //         adjustedVec = vec;
    //         foreach (var hit in hits) {
    //             adjustedVec = Vector3.ProjectOnPlane(adjustedVec, hit.normal);
    //         }

    //         return true;
    //     }

    //     Vector3 remaining = travelVec;
    //     float stepSize = 0.5f;  // half-meter steps
    //     Vector3 movement = rb.position;
    //     while (remaining.sqrMagnitude > 0.0001f) {
    //         Vector3 step = Vector3.ClampMagnitude(remaining, stepSize);
    //         if (CheckForHits(step, out Vector3 safe))
    //             movement += safe;
    //             // rb.MovePosition(rb.position + safe);
    //         else
    //             movement += step;
    //             // rb.MovePosition(rb.position + step);
    //         remaining -= step;
    //     }

    //     rb.MovePosition(movement);
    // }

    // private void UpdateRotation(Quaternion delta) 
    // {
    //     Vector3 pivot = transform.position + localOffset;

    //     Quaternion newRot = delta * rb.rotation;
    //     rb.MoveRotation(newRot);

    //     Vector3 newPos = pivot + delta * (rb.position - pivot);
    //     rb.MovePosition(newPos);

    //     localOffset = pivot - newPos;
    // }

    /// <summary>
    /// This method is responsible for notifying the player that they have picked up a grabbable.
    /// This is the second to last step in the grabbable handshake:
    ///   Player calls StartGrab -> StartGrab calls TargetRpcGrabStateChanged -> HandsImpl
    /// </summary>
    /// <param name="holder">The NetworkConnection interacting with the grabbable</param>
    /// <param name="holderUID">The uid of the player interacting with the grabbable</param>
    /// <param name="grabbable">The grabbable NetworkObject</param>
    [TargetRpc]
    private void TargetRpcGrabStateChanged(NetworkConnection holder, string holderUID, NetworkObject grabbable) 
    {
        Player player = gameplayManager.PlayerObjectManager.GetPlayer(holderUID);
        HandsImpl hands = player.ToolBelt.GetImplementation(Item.NONE) as HandsImpl;
        hands.AcceptGrabStateChange(grabbable);
    }
#endregion

#region Outline
    private Outline CreateOutline() 
    {
        // Find a new Outline
        GameObject outlineObject = gameObject; // Set as self gameObject by default
        IGrabbable grabbable = GetIGrabbable();
        if(grabbable != null && grabbable.GetOutlineObject() != null)
            outlineObject = grabbable.GetOutlineObject();

        if(outlineObject.TryGetComponent(out Outline selectOutlineExisting)) {
            _selectOutline = selectOutlineExisting;
            return selectOutlineExisting;
        }
            
        return outlineObject.AddComponent<Outline>();
    }

    public void UpdateOutline(bool selected = false) 
    {
        // grabbable.SelectOutline.enabled = selected;
        Color selectColor = defaultSelectColor;
        Color normalColor = defaultNormalColor;
        if(Info.OutlineColors.Length == 1) {
            selectColor = Info.OutlineColors[0];
            normalColor = Info.OutlineColors[0];
        } else if(Info.OutlineColors.Length >= 2) {
            selectColor = Info.OutlineColors[0];
            normalColor = Info.OutlineColors[1];
        }

        SelectOutline.OutlineColor = selected ? selectColor : normalColor;
        SelectOutline.OutlineWidth = selected ? 8 : 4;
        SelectOutline.OutlineMode = Outline.Mode.OutlineVisible;
    }
#endregion

}

public interface IGrabbable 
{
    public GrabbableRenderArgs? Render(string viewingPlayer = null) => null;
    public Dictionary<string, string> GetVariables();
    public bool CanPickup(NetworkConnection pickupConnection, string pickupUID, out string reason);
    public GrabbableInfo OverrideGrabbableInfo() => null;
    public GameObject GetOutlineObject() => null;
}