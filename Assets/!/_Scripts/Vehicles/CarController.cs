using System;
using System.Collections;
using System.Collections.Generic;
using EMullen.Core;
using UnityEngine;

public class CarController : MonoBehaviour
{
    protected float horizontalInput, verticalInput;
    protected float currentSteerAngle, currentbreakForce;
    protected bool isBreaking;

    // Settings
    [SerializeField] 
    private float motorForce, breakForce, maxSteerAngle;
    [SerializeField]
    private bool autoBreak = false;
    [SerializeField]
    private bool doFrontWheelsRotate = true;

    // Wheel Colliders
    [SerializeField]
    private float maxSpeed;
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
        HandleMotor();
        HandleSteering();
        UpdateWheels();
        if(!isBreaking)
            KeepUpright();
        LimitSpeed();
    }

    void KeepUpright()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        Quaternion desiredRotation = Quaternion.FromToRotation(transform.up, Vector3.up) * rb.rotation;
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, desiredRotation, 10f * Time.fixedDeltaTime));
    }

    public void SetInput(float verticalInput, float horizontalInput, bool isBreaking) 
    {
        this.horizontalInput = horizontalInput;
        this.verticalInput = verticalInput;
        this.isBreaking = isBreaking;
        if(autoBreak && Mathf.Abs(horizontalInput) < 0.001 && Mathf.Abs(verticalInput) < 0.001)
            this.isBreaking = true;
    }

    private void HandleMotor() {
        rearLeftWheelCollider.motorTorque = verticalInput * motorForce;
        rearRightWheelCollider.motorTorque = verticalInput * motorForce;
        currentbreakForce = isBreaking ? breakForce : 0f;
        ApplyBreaking();
    }

    private void LimitSpeed()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        if (horizontalVelocity.magnitude > maxSpeed)
        {
            Vector3 limitedVelocity = horizontalVelocity.normalized * maxSpeed;
            rb.velocity = new Vector3(limitedVelocity.x, rb.velocity.y, limitedVelocity.z);
        }
    }

    private void ApplyBreaking() {
        if(frontRightWheelCollider != null) frontRightWheelCollider.brakeTorque = currentbreakForce;
        if(frontLeftWheelCollider != null) frontLeftWheelCollider.brakeTorque = currentbreakForce;
        if(rearLeftWheelCollider != null) rearLeftWheelCollider.brakeTorque = currentbreakForce;
        if(rearRightWheelCollider != null) rearRightWheelCollider.brakeTorque = currentbreakForce;
    }

    private void HandleSteering() {
        currentSteerAngle = maxSteerAngle * horizontalInput;
        if(frontLeftWheelCollider != null) frontLeftWheelCollider.steerAngle = currentSteerAngle;
        if(frontRightWheelCollider != null) frontRightWheelCollider.steerAngle = currentSteerAngle;
    }

    private void UpdateWheels() {
        if(frontLeftWheelCollider != null && frontLeftWheelTransform != null) UpdateSingleWheel(frontLeftWheelCollider, frontLeftWheelTransform);
        if(frontRightWheelCollider != null && frontRightWheelTransform != null) UpdateSingleWheel(frontRightWheelCollider, frontRightWheelTransform);
        if(rearRightWheelCollider != null && rearRightWheelTransform != null) UpdateSingleWheel(rearRightWheelCollider, rearRightWheelTransform);
        if(rearLeftWheelCollider != null && rearLeftWheelTransform != null) UpdateSingleWheel(rearLeftWheelCollider, rearLeftWheelTransform);
    }

    private void UpdateSingleWheel(WheelCollider wheelCollider, Transform wheelTransform) {
        Vector3 pos;
        Quaternion rot; 
        wheelCollider.GetWorldPose(out pos, out rot);
        if(doFrontWheelsRotate)
            wheelTransform.rotation = rot;
        wheelTransform.position = pos;
    }
}