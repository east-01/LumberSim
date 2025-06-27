using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraManager : MonoBehaviour, IInputListener
{
    public Mode mode;
    public bool Locked = false;

    [SerializeField]
    private FirstPersonCamera firstPersonCamera;
    [SerializeField]
    private ThirdPersonCamera thirdPersonCamera;

    private void Start()
    {
        mode = Mode.FIRST_PERSON;   
    }

    private void Update()
    {
        firstPersonCamera.enabled = mode == Mode.FIRST_PERSON;
        thirdPersonCamera.enabled = mode == Mode.THIRD_PERSON;
    }

    public void InputEvent(InputAction.CallbackContext context)
    {

    }

    public void InputPoll(InputAction action)
    {
        if(Input.GetKeyDown(KeyCode.Escape)) {
            Locked = !Locked;
            Debug.Log("Locked: " + Locked);
        }

        firstPersonCamera.input = Vector2.zero;
        thirdPersonCamera.input = Vector2.zero;

        if(Locked)
            return;

        if(action.name == "Look") {
            Vector2 input = action.ReadValue<Vector2>();
            switch(mode) {
                case Mode.FIRST_PERSON:
                    firstPersonCamera.input = input;
                    break;
                case Mode.THIRD_PERSON:
                    thirdPersonCamera.input = input;
                    break;
                case Mode.WORLD:
                    break;
            }
        }
    }

    public enum Mode { FIRST_PERSON, THIRD_PERSON, WORLD }

}
