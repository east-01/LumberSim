using System;
using EMullen.Core;
using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

public class VehicleDriver : MonoBehaviour, IInputListener
{
    public Vector2 movementInput;
    public bool sprintingInput;
    public bool jumpInput;

    private Player player;
    private CharacterController characterController;

    private NetworkObject drivingVehicle;
    [SerializeField] private string drivingVehicleReadout;
    public bool isDriving => drivingVehicle != null;

    private void Start()
    {
        player = GetComponent<Player>();   
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogWarning("CharacterController is missing on this GameObject. Adding one.");
            characterController = gameObject.AddComponent<CharacterController>();
        }
    }

    public void Update() 
    {
        drivingVehicleReadout = drivingVehicle != null ? drivingVehicle.ToString() : "null";
        if(drivingVehicle != null) {
            transform.position = drivingVehicle.transform.position;
            // characterController.Move(drivingVehicle.transform.position-transform.position);

            drivingVehicle.GetComponent<Vehicle>().CarController.SetInput(movementInput.y, movementInput.x, jumpInput);
        }
    }

    public void SetDrivingVehicle(NetworkObject vehicleNob) 
    {
        if(vehicleNob == null) 
            throw new InvalidOperationException($"Can't set driving vehicle null vehicleNob passed in.");

        characterController.enabled = false;
        player.CameraManager.mode = CameraManager.Mode.THIRD_PERSON;

        if(!vehicleNob.TryGetComponent(out Vehicle vehicle))
            throw new InvalidOperationException($"Failed to get Vehicle from vehicle NetworkObject");

        vehicle.SetDriver(player.uid.Value);
        drivingVehicle = vehicleNob;
    }

    public void ClearDrivingVehicle() 
    {
        if(drivingVehicle == null)
            throw new InvalidOperationException("Can't clear driving vehicle, the drivingVehicle is null.");

        if(!drivingVehicle.TryGetComponent(out Vehicle vehicle))
            throw new InvalidOperationException($"Failed to get Vehicle from vehicle NetworkObject");

        Vector3 targetPos = drivingVehicle.transform.position + -drivingVehicle.transform.right*2.5f;
        transform.position = targetPos;

        characterController.enabled = true;
        player.CameraManager.mode = CameraManager.Mode.FIRST_PERSON;

        vehicle.SetDriver(null);
        drivingVehicle = null;

        // characterController.Move(targetPos-transform.position);
    }

    public void InputEvent(InputAction.CallbackContext context) {}

    public void InputPoll(InputAction action)
    {
        if(action.name == "Move") {
            movementInput = action.ReadValue<Vector2>(); // Direction of movement input
        } else if(action.name == "Sprint") {
            sprintingInput = action.ReadValue<float>() > 0.1f;
        } else if(action.name == "Jump") {
            jumpInput = action.ReadValue<float>() > 0.1f;
        } 
    }
}