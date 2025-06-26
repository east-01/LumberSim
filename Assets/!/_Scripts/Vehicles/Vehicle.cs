
using System.Collections.Generic;
using EMullen.Core;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

/// <summary>
/// Connects players to control the vehicle.
/// </summary>
[RequireComponent(typeof(CarController))]
public class Vehicle : NetworkBehaviour, IGrabbable
{
    private readonly SyncVar<string> driverUID = new();

    public CarController CarController { get; private set; }

    private void Awake() 
    {
        CarController = GetComponent<CarController>();
    }

    private void Update()
    {
        if(!HasDriver())
            CarController.SetInput(0, 0, true);   
    }

    public bool HasDriver() {
        return driverUID.Value != null && driverUID.Value != "";
    }

    public void SetDriver(string driverUID) 
    {
        if(HasDriver() && driverUID != null) {
            Debug.LogError("Can't set driver, Vehicle already has driver.");
            return;
        }

        this.driverUID.Value = driverUID;
    }

    public bool CanPickup(NetworkConnection pickupConnection, string pickupUID, out string reason) 
    {
        reason = "";
        return false;
    }

    public Dictionary<string, string> GetVariables()
    {
        return new();
    }
}