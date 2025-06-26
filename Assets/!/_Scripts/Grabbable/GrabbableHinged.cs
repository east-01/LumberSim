using System;
using System.Collections.Generic;
using EMullen.Core;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;

public class GrabbableHinged : Grabbable, IGrabbable
{
    [Header("Pivot‐Rotation Settings")]
    [Tooltip("Allow free rotation around the grab point.")]
    [SerializeField] private bool usePointPivot = true;

    [Header("Character Controller Settings")]
    [Tooltip("Slide across surfaces instead of full physics.")]
    [SerializeField] private bool useCharacterController = false;
    [SerializeField] private float controllerSkinWidth = 0.1f;
    [SerializeField] private float controllerSlopeLimit = 45f;

    private CharacterController _charController;
    private bool _isPointPivotActive;
    private Vector3 _pivotLocal;

    private void Awake()
    {
        if (useCharacterController)
        {
            // remove Rigidbody so CharacterController drives movement
            Destroy(GetComponent<Rigidbody>());
            _charController = gameObject.AddComponent<CharacterController>();
            _charController.skinWidth = controllerSkinWidth;
            _charController.slopeLimit = controllerSlopeLimit;
        }
    }

    public override void StartGrab(NetworkConnection grabber, string grabberUID, Vector3 hitPoint)
    {
        base.StartGrab(grabber, grabberUID, hitPoint);

        if (usePointPivot)
        {
            _isPointPivotActive = true;
            // store the local‐space pivot so we can reconstruct world pivot each frame
            _pivotLocal = transform.InverseTransformPoint(hitPoint);
        }
    }

    public override void UpdatePositionAndRotation(Vector3 targetPosition, Quaternion rotationDelta)
    {
        if (useCharacterController)
        {
            // Smooth slide toward the hand
            Vector3 move = (targetPosition - transform.position);
            _charController.Move(move);

            // Also apply any rotation from the hand
            transform.rotation = rotationDelta * transform.rotation;
        }
        else if (usePointPivot && _isPointPivotActive)
        {
            // 1) Compute world‐space pivot
            Vector3 worldPivot = transform.TransformPoint(_pivotLocal);

            // 2) Rotate around that pivot
            //    - Rotate the orientation:
            Quaternion newRot = rotationDelta * GetComponent<Rigidbody>().rotation;
            GetComponent<Rigidbody>().MoveRotation(newRot);

            //    - Rotate the position around the pivot:
            Vector3 offset = GetComponent<Rigidbody>().position - worldPivot;
            Vector3 rotatedOffset = rotationDelta * offset;
            Vector3 newPos = worldPivot + rotatedOffset;
            GetComponent<Rigidbody>().MovePosition(newPos);
        }
        else
        {
            // fallback to default Grabbable behavior
            base.UpdatePositionAndRotation(targetPosition, rotationDelta);
        }
    }

    public override void StopGrab()
    {
        base.StopGrab();
        _isPointPivotActive = false;
    }

    // Minimal IGrabbable boilerplate
    public GrabbableRenderArgs? Render(string viewingPlayer = null) => null;
    public Dictionary<string, string> GetVariables() => new Dictionary<string, string>();
    public bool CanPickup(NetworkConnection pickupConnection, string pickupUID, out string reason)
    {
        reason = null;
        return true;
    }
    public GameObject GetOutlineObject() => gameObject;
}
