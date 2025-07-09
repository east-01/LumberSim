using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasCameraScaler : MonoBehaviour
{
    [SerializeField]
    private Canvas targetCanvas;

    [SerializeField]
    private float widthPadding;
    [SerializeField]
    private float heightPadding;

    void Start()
    {
        if (targetCanvas.renderMode != RenderMode.WorldSpace)
        {
            Debug.LogError("Canvas must be in World Space render mode.");
            return;
        }

        // Get the Camera component
        Camera cam = GetComponent<Camera>();
        if (!cam.orthographic)
        {
            Debug.LogError("Camera must be Orthographic.");
            return;
        }

        // Get the RectTransform of the Canvas
        RectTransform rt = targetCanvas.GetComponent<RectTransform>();

        // Compute the world‐space size of the RectTransform
        // Note: lossyscale accounts for any parent scaling
        Vector3 lossyScale = rt.lossyScale;
        float worldWidth  = (rt.rect.width + widthPadding)  * lossyScale.x;
        float worldHeight = (rt.rect.height + heightPadding) * lossyScale.y;

        // Camera.orthographicSize is half of the vertical size of the view
        // To ensure the entire canvas fits, we need:
        //   orthographicSize >= worldHeight/2
        // but if the canvas is very wide, we may need to base size on width:
        //   orthographicSize >= (worldWidth / cam.aspect) / 2
        float sizeForHeight = worldHeight / 2f;
        float sizeForWidth  = (worldWidth / cam.aspect) / 2f;
        cam.orthographicSize = Mathf.Max(sizeForHeight, sizeForWidth);

        // Optional: point the camera at the center of the canvas
        // and position it so the canvas plane is in view.
        Transform canvasT = targetCanvas.transform;
        Vector3 canvasCenter = canvasT.position; 
        Vector3 canvasNormal = canvasT.forward;  // assuming Canvas is facing +Z
        float distance = 10f; // or whatever your setup needs
        cam.transform.position = canvasCenter - canvasNormal * distance;
        cam.transform.rotation = Quaternion.LookRotation(canvasNormal, canvasT.up);
    }
}
