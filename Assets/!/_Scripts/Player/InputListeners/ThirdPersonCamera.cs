using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("References")]
    public Transform target;

    [Header("Orbit Settings")]
    public Vector2 input; // External input: (x = horizontal, y = vertical)
    public float distance = 5f;
    public float sensitivity = 3f;
    public float smoothTime = 0.1f;

    [Header("Vertical Rotation Limits")]
    public float minVerticalAngle = -30f;
    public float maxVerticalAngle = 60f;

    private Vector2 currentRotation;
    private Vector2 rotationVelocity;

    private void LateUpdate()
    {
        if (!target) return;

        // Add external input to rotation (scaled by sensitivity)
        currentRotation.x += input.x * sensitivity;
        currentRotation.y -= input.y * sensitivity; // invert Y if desired

        // Clamp vertical rotation
        currentRotation.y = Mathf.Clamp(currentRotation.y, minVerticalAngle, maxVerticalAngle);

        // Smooth rotation
        Vector2 smoothRotation = Vector2.SmoothDamp(
            transform.eulerAngles, 
            currentRotation, 
            ref rotationVelocity, 
            smoothTime);

        // Calculate rotation and position
        Quaternion rotation = Quaternion.Euler(currentRotation.y, currentRotation.x, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -distance);

        transform.position = target.position + offset;
        transform.LookAt(target);
    }
}
