using System;
using System.Collections;
using System.Collections.Generic;
using EMullen.Core;
using UnityEngine;

public class CarController : MonoBehaviour
{
    private float horizontalInput, verticalInput;
    private float currentSteerAngle, currentbreakForce;
    private bool isBreaking;

    // Settings
    [SerializeField] 
    private float motorForce, breakForce, maxSteerAngle;

    // Wheel Colliders
    [SerializeField] 
    private WheelCollider frontLeftWheelCollider, frontRightWheelCollider;
    [SerializeField] 
    private WheelCollider rearLeftWheelCollider, rearRightWheelCollider;

    // Wheels
    [SerializeField] 
    private Transform frontLeftWheelTransform, frontRightWheelTransform;
    [SerializeField] 
    private Transform rearLeftWheelTransform, rearRightWheelTransform;

    private void FixedUpdate() {
        GetInput();
        HandleMotor();
        HandleSteering();
        UpdateWheels();
    }

    private void GetInput() {
        verticalInput = 0f;
        horizontalInput = 0f;

        Vector3 direction = Vector3.zero;
        if(Input.GetKey(KeyCode.U)) {
            verticalInput = 1f;
        } 
        
        if(Input.GetKey(KeyCode.H)) {
            horizontalInput = -1f;
        } 
        
        if(Input.GetKey(KeyCode.J)) {
            verticalInput = -1f;
        } 
        
        if(Input.GetKey(KeyCode.K)) {
            horizontalInput = 1f;
        }

        BLog.Highlight($"Hor: {horizontalInput} ver: {verticalInput}");

        // Breaking Input
        isBreaking = Input.GetKey(KeyCode.Space);
    }

    private void HandleMotor() {
        frontLeftWheelCollider.motorTorque = verticalInput * motorForce;
        frontRightWheelCollider.motorTorque = verticalInput * motorForce;
        currentbreakForce = isBreaking ? breakForce : 0f;
        ApplyBreaking();
    }

    private void ApplyBreaking() {
        frontRightWheelCollider.brakeTorque = currentbreakForce;
        frontLeftWheelCollider.brakeTorque = currentbreakForce;
        rearLeftWheelCollider.brakeTorque = currentbreakForce;
        rearRightWheelCollider.brakeTorque = currentbreakForce;
    }

    private void HandleSteering() {
        currentSteerAngle = maxSteerAngle * horizontalInput;
        frontLeftWheelCollider.steerAngle = currentSteerAngle;
        frontRightWheelCollider.steerAngle = currentSteerAngle;
    }

    private void UpdateWheels() {
        UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelTransform);
        UpdateSingleWheel(frontRightWheelCollider, frontRightWheelTransform);
        UpdateSingleWheel(rearRightWheelCollider, rearRightWheelTransform);
        UpdateSingleWheel(rearLeftWheelCollider, rearLeftWheelTransform);
    }

    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform) {
        Vector3 pos;
        Quaternion rot; 
        wheelCollider.GetWorldPose(out pos, out rot);
        wheelTransform.rotation = rot;
        wheelTransform.position = pos;
    }
}