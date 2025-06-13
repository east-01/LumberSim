using FishNet;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

public class HandsImpl : ToolBeltImpl 
{
    [SerializeField]
    private new Camera camera;

    private Player player;

    [SerializeField]
    private float grabDistance = 5;
    [SerializeField]
    private float maxCarryWeight = 60;

    private Grabbable grabbed;
    private Vector3 grabOffset; // The offset of grab point - position
    private Vector3 targetPosition; // The target place the camera is pointing to.

    public Grabbable LastSelectedGrabbable { get; private set; }
    
    private void Awake()
    {
        player = GetComponentInParent<Player>();   
    }

    private void Update()
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
        }
    }

    public void UpdateGrabbedGroupPosition(NetworkObject grabbedGroup, Vector3 targetPosition) 
    {
        if(!InstanceFinder.IsServerStarted) {
            // grabbedGroup.transform.position = Vector3.Lerp(grabbedGroup.transform.position, targetPosition, Vector3.Distance(grabbedGroup.transform.position, targetPosition)/10f);
            ServerRpcUpdateGrabbedGroupPosition(grabbedGroup, targetPosition);
            return;
        }

        Rigidbody rb = grabbedGroup.GetComponent<Rigidbody>();
        Vector3 newPos = Vector3.Lerp(grabbedGroup.transform.position, targetPosition, Mathf.Max(Vector3.Distance(grabbedGroup.transform.position, targetPosition)/10f, 0.5f));
        rb.MovePosition(newPos);
        // rb.AddForce((targetPosition - grabbedGroup.transform.position).normalized * moveSpeed * Time.deltaTime, ForceMode.VelocityChange);
    }
    [ServerRpc(RequireOwnership = false)]
    public void ServerRpcUpdateGrabbedGroupPosition(NetworkObject grabbedGroup, Vector3 targetPosition) => UpdateGrabbedGroupPosition(grabbedGroup, targetPosition);

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
            if(grabbableInterface != null && !grabbableInterface.CanPickup(LocalConnection)) {
                player.GetHUD().ShowWarning("Can't pickup", 2f);
                return;
            }

            player.GetNetworkedAudioController().PlaySound("pickup");

            grabbed = grabbable;
            grabOffset = hit.point-grabbed.transform.position; // TODO: Implement offset and rotation

            if(grabbed.TryGetComponent(out Rigidbody grabbedRB))
                grabbedRB.useGravity = false;

            ServerSetPickedUp(grabbed.ObjectId, performed, LocalConnection);
        } else {
            if(grabbed != null && grabbed.TryGetComponent(out Rigidbody grabbedRB)) {
                grabbedRB.useGravity = true;
                Vector3 targVel = grabbed.GetComponent<Grabbable>().SmoothedPerceivedVelocity;
                grabbedRB.velocity = targVel;
            }

            if(grabbed != null)
                ServerSetPickedUp(grabbed.ObjectId, performed, LocalConnection);

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