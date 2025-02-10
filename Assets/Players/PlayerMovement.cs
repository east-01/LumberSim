using EMullen.Core;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{

    public PlayerInput input;
    public Vector2 MovementInput => input.actions["Move"].ReadValue<Vector2>(); // Direction of movement input
    public bool SprintingInput => input.actions["Sprint"].ReadValue<float>() > 0.1f;
    public bool JumpInput => input.actions["Jump"].ReadValue<float>() > 0.1f;
    private bool lastJump;
    public bool JumpDown;

    [Header("Movement Settings")]
    public float moveSpeed = 5f; // Speed of movement
    public float sprintSpeed = 9f;
    public float speedTransitionTime = 0.35f;
    
    /// <summary>
    /// The actual speed of the player.
    /// </summary>
    public float speed { get; private set; }
    private float _targetSpeed;
    private float targetSpeed { 
        get => _targetSpeed;
        set { 
            targetSpeedChangeInitialSpeed = speed;
            targetSpeedChangeTime = Time.time;
            _targetSpeed = value;
        }
    }
    /// <summary>
    /// The initial speed before the target change
    /// </summary>
    private float targetSpeedChangeInitialSpeed;
    private float targetSpeedChangeTime;

    public float jumpPower = 40f;
    public float jumpDecay = 0.65f;
    public float jumpPowerRemaining { get; private set; }

    private CharacterController characterController;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogWarning("CharacterController is missing on this GameObject. Adding one.");
            characterController = gameObject.AddComponent<CharacterController>();
        }
    }

    void Update()
    {
        if(input == null)
            return;

        // Check if the jump button was just pressed
        JumpDown = !lastJump && JumpInput;
        if(JumpDown)
            jumpPowerRemaining = 13f;

        if(Time.time - targetSpeedChangeTime < speedTransitionTime) {
            speed = Mathf.Lerp(targetSpeedChangeInitialSpeed, targetSpeed, (Time.time-targetSpeedChangeTime)/speedTransitionTime);
        }

        HandleMovement();
        
        // Reduce jump power over time
        if (jumpPowerRemaining > 0)
        {
            jumpPowerRemaining -= jumpDecay * Time.deltaTime;
            if (jumpPowerRemaining < 0)
                jumpPowerRemaining = 0;
        }

        lastJump = JumpInput;
    }

    private void HandleMovement()
    {
        // The target speed this tick
        float tickTargetSpeed = SprintingInput ? sprintSpeed : moveSpeed;
        if(tickTargetSpeed != targetSpeed)
            targetSpeed = tickTargetSpeed;

        Vector3 forward = MovementInput.y * transform.forward * speed * Time.deltaTime;
        Vector3 horizontal = MovementInput.x * (Quaternion.Euler(0, 90, 0)*transform.forward) * speed * Time.deltaTime;
        Vector3 vertical = (Physics.gravity + transform.up*jumpPowerRemaining) * Time.deltaTime;
        characterController.Move(forward+horizontal+vertical);
    }
}
