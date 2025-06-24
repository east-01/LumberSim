using System;
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
    private float rotationSpeed = 5f;
    [SerializeField]
    private float rotationSensitivity = 15f;
    [SerializeField]
    private float rotationAcceleration = 0.25f;
    private float currentRotationSpeed;

    private Grabbable grabbed;
    private Vector3 targetPosition; // The target place the camera is pointing to.
    /// <summary>
    /// This variable is set in the InputPoll Look section, and is consumed on the next frame.
    /// Consumption works by setting the rotationDelta to the quaternion identity.
    /// </summary>
    private Quaternion rotationDelta;
    
    private bool rotatingItem = false;

    private void Awake()
    {
        player = GetComponentInParent<Player>();   

        rotationDelta = Quaternion.identity;
    }

    private void Update()
    {
        currentRotationSpeed = Mathf.Max(currentRotationSpeed - (rotationAcceleration * Time.deltaTime), 0);
    }

    private void FixedUpdate()
    {
        if(grabbed == null)
            return;

        targetPosition = camera.transform.position + camera.transform.forward * grabDistance;

        grabbed.UpdatePositionAndRotation(targetPosition, rotationDelta);

        rotationDelta = Quaternion.identity; // Consume rotation delta
    }

    public override void HandleInput(Item item, InputAction.CallbackContext context)
    {
        if(context.action.name == "Primary") {
            PickupGrabbable(context.performed);
        } else if(context.action.name == "Secondary") {
            rotatingItem = context.performed;
            player.FirstPersonCamera.Locked = context.performed;
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

            Quaternion yaw   = Quaternion.AngleAxis(rotationY, Vector3.up);
            Quaternion pitch = Quaternion.AngleAxis(-rotationX, camera.transform.right);
            rotationDelta = pitch * yaw;
        }
    }

    /// <summary>
    /// Pick up/drop a log, has separate actions for 
    ///   performed=true: Try to pick up a log and store a grabbedGroup
    ///   performed=false: Drop the current grabbedGroup if it exists
    /// </summary>
    /// <param name="performed">Performed boolean</param>
    private void PickupGrabbable(bool performed) 
    {
        if(performed) {

            if(grabbed != null)
                return;

            Grabbable grabbable = player.GrabbablePicker.PickGrabbable(grabDistance, out RaycastHit hit);

            if(grabbable == null)
                return;

            // Vector3 grabOffset = hit.point-grabbable.transform.position;
            grabbable.StartGrab(LocalConnection, player.uid.Value, hit.point);

        } else {
            
            if(grabbed == null)
                return;

            grabbed.StopGrab();

        }
    }

    public void AcceptGrabStateChange(NetworkObject grabbable) 
    {
        if(grabbable != null) {
     
            if(grabbed != null)
                throw new InvalidOperationException("Can't AcceptGrabStateChange, getting a new grabbable while we already have one.");
            
            player.GetNetworkedAudioController().PlaySound("pickup");

            grabbed = grabbable.GetComponent<Grabbable>();
            player.GrabbablePicker.SetSelectedGrabbable(grabbed, true);

        } else {

            grabbed = null;
            player.GrabbablePicker.ClearSelectedGrabbable();

        }
    }
}