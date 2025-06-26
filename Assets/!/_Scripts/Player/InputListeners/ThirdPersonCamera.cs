using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Offsets")]
    public float distance = 5f;
    public float height   = 2f;

    [Header("Speeds")]
    public float followSpeed   = 10f;
    public float rotationSpeed = 5f;

    [Header("External Input")]
    // Set this from any other script before LateUpdate runs
    public Vector2 input;

    private float yaw;
    private float pitch;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("ThirdPersonCamera: No target assigned!");
            enabled = false;
            return;
        }

        // init from current orientation
        Vector3 angles = transform.eulerAngles;
        yaw   = angles.y;
        pitch = angles.x;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    void LateUpdate()
    {
        // apply external input
        Vector2 look = input;

        // optionally clear so it doesn't accumulate if you forget to set it
        input = Vector2.zero;

        // orbit math
        yaw   += look.x * rotationSpeed;
        pitch  = Mathf.Clamp(pitch - look.y * rotationSpeed, -35f, 60f);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPos = target.position
                             - rot * Vector3.forward * distance
                             + Vector3.up * height;

        // smooth follow
        transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, rot,             followSpeed * Time.deltaTime);
    }
}
