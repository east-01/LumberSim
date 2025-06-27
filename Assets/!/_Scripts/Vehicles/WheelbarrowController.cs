using System;
using System.Collections;
using System.Collections.Generic;
using EMullen.Core;
using UnityEngine;

public class WheelbarrowController : CarController
{
    public float targetHeight = 2f;
    public float heightLerpSpeed = 10f;
    public float uprightLerpSpeed = 10f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        MaintainHeight();
        MaintainUprightRotation();
    }

    void MaintainHeight()
    {
        Ray ray = new Ray(transform.position, -Vector3.up);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 desiredPosition = new Vector3(transform.position.x, hit.point.y + targetHeight, transform.position.z);
            Vector3 newPosition = Vector3.Lerp(transform.position, desiredPosition, Time.fixedDeltaTime * heightLerpSpeed);
            rb.MovePosition(newPosition);
        }
    }

    void MaintainUprightRotation()
    {
        Quaternion desiredRotation = Quaternion.FromToRotation(transform.up, Vector3.up) * rb.rotation;
        Quaternion newRotation = Quaternion.Slerp(rb.rotation, desiredRotation, Time.fixedDeltaTime * uprightLerpSpeed);
        rb.MoveRotation(newRotation);
    }
}