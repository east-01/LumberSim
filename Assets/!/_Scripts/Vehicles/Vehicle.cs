
using System.Collections.Generic;
using EMullen.Core;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

/// <summary>
/// Connects players to control the vehicle.
/// </summary>
public class Vehicle : NetworkBehaviour
{
    private readonly SyncVar<string> driverUID = new();
    [SerializeField]
    private Transform playerAttachPoint;
    public Transform PlayerAttachPoint => playerAttachPoint;

    [SerializeField]
    private CarController carController;
    public CarController CarController => carController;

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
}