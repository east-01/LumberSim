using EMullen.Core;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

public class HandsImpl : ToolBeltImpl, IInputListener
{
    [SerializeField]
    private new Camera camera;

    private Player player;

    [SerializeField]
    private float grabDistance = 5;
    [SerializeField]
    private float maxCarryWeight = 60;
    [SerializeField]
    private float rotationSpeed = 5f;
    [SerializeField]
    private float rotationSensitivity = 15f;
    [SerializeField]
    private float rotationAcceleration = 0.25f;
    private float currentRotationSpeed;

    private Grabbable grabbed;
    private Rigidbody grabbedRB;
    private Vector3 grabOffset; // The offset of grab point - position
    private Vector3 targetPosition; // The target place the camera is pointing to.

    public Grabbable LastSelectedGrabbable { get; private set; }
    
    private bool rotatingItem = false;

    private void Awake()
    {
        player = GetComponentInParent<Player>();   
    }

    private void Update()
    {
        currentRotationSpeed = Mathf.Max(currentRotationSpeed - (rotationAcceleration * Time.deltaTime), 0);
    }

    private void FixedUpdate()
    {
        if(grabbed != null) {
            targetPosition = -grabOffset + (camera.transform.position + camera.transform.forward * grabDistance);
            UpdateGrabbedGroupPosition(grabbed.NetworkObject, targetPosition);
        }   
    }

    public override void HandleInput(Item item, InputAction.CallbackContext context)
    {
        if(context.action.name == "Primary") {
            PickupGrabbable(context.performed);
        } else if(context.action.name == "Secondary") {
            rotatingItem = context.performed;
            player.FirstPersonCamera.Locked = context.performed;
            BLog.Highlight($"Rotating item: {rotatingItem}");
        }
    }

    public void InputEvent(InputAction.CallbackContext context) {}

    public void InputPoll(InputAction action)
    {
        if(action.name == "Look") {
            if(!rotatingItem || grabbed == null)
                return;

            Vector2 rotation = action.ReadValue<Vector2>();
            float inputMagnitude = rotation.magnitude;
            rotation = rotation.normalized * Mathf.Clamp01(rotation.magnitude / rotationSensitivity);

            if (inputMagnitude > 0f) {
                // accelerate toward max speed
                currentRotationSpeed = Mathf.Min(
                    currentRotationSpeed + rotationAcceleration * Time.deltaTime * inputMagnitude,
                    rotationSpeed
                );
            }
        
            // Convert to rotation angles (you can invert Y if needed)
            float rotationX = rotation.y * currentRotationSpeed;
            float rotationY = rotation.x * currentRotationSpeed;

            UpdateRotation(grabbed.NetworkObject, grabOffset, rotationX, rotationY);
        }
    }

    // private void UpdateRotation(NetworkObject grabbedGroup, Vector3 offset, float rotationX, float rotationY) 
    // {
    //     // grabbedGroup.transform.RotateAround(offset, Vector3.up, rotationY);    // Rotate around Y-axis (horizontal movement)
    //     // grabbedGroup.transform.RotateAround(offset, Vector3.right, -rotationX); // Rotate around X-axis (vertical movement)

    //     // grabbed.transform.Rotate(Vector3.up, rotationY, Space.World);    // Rotate around Y-axis (horizontal movement)
    //     // grabbed.transform.Rotate(Vector3.right, -rotationX, Space.Self); // Rotate around X-axis (vertical movement)

    //     Vector3 pivot = grabbedGroup.transform.position + offset;

    //     grabbedGroup.transform.RotateAround(pivot, Vector3.up, rotationY);
    //     grabbedGroup.transform.RotateAround(pivot, camera.transform.right, -rotationX);

    //     if(!InstanceFinder.IsServerStarted)
    //         ServerRPCUpdateRotation(grabbedGroup, offset, rotationX, rotationY);
    // }

    private void UpdateRotation(NetworkObject grabbedGroup, Vector3 offset, float rotationX, float rotationY)
    {
        // world‐space pivot point
        Vector3 pivot = grabbedGroup.transform.position + offset;

        // build a single delta‐rotation (yaw around world‐up, then tilt around camera‐right)
        Quaternion yaw   = Quaternion.AngleAxis(rotationY, Vector3.up);
        Quaternion pitch = Quaternion.AngleAxis(-rotationX, camera.transform.right);
        Quaternion delta = pitch * yaw;

        // apply to Rigidbody in FixedUpdate loop
        // 1) new orientation
        Quaternion newRot = delta * grabbedRB.rotation;
        grabbedRB.MoveRotation(newRot);

        // 2) new position so we still orbit around the pivot
        Vector3 newPos = pivot + delta * (grabbedRB.position - pivot);
        grabbedRB.MovePosition(newPos);

        // 3) update offset so FixedUpdate’s follow‐target stays at this grab‐point
        grabOffset = pivot - newPos;

        // mirror to server
        if (!InstanceFinder.IsServerStarted)
            ServerRPCUpdateRotation(grabbedGroup, grabOffset, rotationX, rotationY);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerRPCUpdateRotation(NetworkObject grabbedGroup, Vector3 offset, float rotationX, float rotationY) => UpdateRotation(grabbedGroup, offset, rotationX, rotationY);

    public void UpdateGrabbedGroupPosition(NetworkObject grabbedGroup, Vector3 targetPosition) 
    {
        Rigidbody rb = grabbedGroup.GetComponent<Rigidbody>();
        Vector3 startPos = grabbedGroup.transform.position;
        float distToTarget = Vector3.Distance(grabbedGroup.transform.position, targetPosition);
        Vector3 newPos = Vector3.Lerp(startPos, targetPosition, Mathf.Max(distToTarget/10f, 0.05f));
        Vector3 travelVec = newPos-startPos;

        bool CheckForHits(Vector3 vec, out Vector3 adjustedVec) 
        {
            RaycastHit[] hits = rb.SweepTestAll(vec.normalized, vec.magnitude, QueryTriggerInteraction.Ignore);

            if(hits.Length == 0) {
                adjustedVec = vec;
                return false;
            }

            if (hits.Length > 1)
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            adjustedVec = vec;
            foreach (var hit in hits) {
                adjustedVec = Vector3.ProjectOnPlane(adjustedVec, hit.normal);
            }

            return true;
        }

        Vector3 remaining = travelVec;
        float stepSize = 0.5f;  // half-meter steps
        Vector3 movement = rb.position;
        while (remaining.sqrMagnitude > 0.0001f)
        {
            Vector3 step = Vector3.ClampMagnitude(remaining, stepSize);
            if (CheckForHits(step, out Vector3 safe))
                movement += safe;
                // rb.MovePosition(rb.position + safe);
            else
                movement += step;
                // rb.MovePosition(rb.position + step);
            remaining -= step;
        }

        rb.MovePosition(movement);

        if(!InstanceFinder.IsServerStarted)
            ServerRpcUpdateGrabbedGroupPosition(grabbedGroup, movement);
    }
    [ServerRpc(RequireOwnership = false)]
    public void ServerRpcUpdateGrabbedGroupPosition(NetworkObject grabbedGroup, Vector3 targetPosition) => grabbedGroup.GetComponent<Rigidbody>().MovePosition(targetPosition);

    /// <summary>
    /// Pick up/drop a log, has separate actions for 
    ///   performed=true: Try to pick up a log and store a grabbedGroup
    ///   performed=false: Drop the current grabbedGroup if it exists
    /// </summary>
    /// <param name="performed">Performed boolean</param>
    private void PickupGrabbable(bool performed) 
    {
        if(performed) {

            Grabbable grabbable = player.GrabbablePicker.PickGrabbable(grabDistance, out RaycastHit hit);

            if(grabbable == null)
                return;

            IGrabbable grabbableInterface = grabbable.GetIGrabbable();
            if(grabbableInterface != null && !grabbableInterface.CanPickup(LocalConnection, player.uid.Value, out string reason)) {
                bool hasReason = reason != null && reason.Length > 0;
                player.GetHUD().ShowWarning("Can't pickup" + (hasReason ? $": {reason}" : ""), 2f);
                return;
            }

            player.GetNetworkedAudioController().PlaySound("pickup");

            grabbed = grabbable;
            grabOffset = hit.point-grabbed.transform.position;

            player.GrabbablePicker.SetSelectedGrabbable(grabbable, true);

            if(grabbed.TryGetComponent(out Rigidbody grabbedRB)) {
                grabbedRB.useGravity = false;
                this.grabbedRB = grabbedRB;
            }

            ServerSetPickedUp(grabbed.ObjectId, performed, LocalConnection);
        } else {
            if(grabbed != null && grabbed.TryGetComponent(out Rigidbody grabbedRB)) {
                grabbedRB.useGravity = true;
                Vector3 targVel = grabbed.GetComponent<Grabbable>().SmoothedPerceivedVelocity;
                grabbedRB.velocity = targVel;
            }

            if(grabbed != null)
                ServerSetPickedUp(grabbed.ObjectId, performed, LocalConnection);

            player.GrabbablePicker.ClearSelectedGrabbable();
            grabbed = null;
            grabOffset = Vector3.zero;
        }
    }

    private void ServerSetPickedUp(int logNetID, bool performed, NetworkConnection performer) 
    {
        if(!InstanceFinder.IsServerStarted) {
            ServerRpcServerSetPickedUp(logNetID, performed, InstanceFinder.ClientManager.Connection);
            return;
        }

        Grabbable group = player.GameplayManager.GetGrabbableFromNetworkID(logNetID);

        if(!group.GetComponent<NetworkObject>().Owner.IsValid) {
            group.GetComponent<NetworkObject>().GiveOwnership(performer);
        }

        Rigidbody rb = group.GetComponent<Rigidbody>();
        rb.useGravity = !performed;

    }
    [ServerRpc(RequireOwnership = false)]
    private void ServerRpcServerSetPickedUp(int logNetID, bool performed, NetworkConnection performer) => ServerSetPickedUp(logNetID, performed, performer);
}