using EMullen.Core;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class ToolBelt : MonoBehaviour, IInputListener
{

    [SerializeField]
    private new Camera camera;
    [SerializeField]
    private TMP_Text toolbeltText;

    int toolbeltIndex = 0;
    string[] toolbeltOptions = new string[] {"hands", "axe"};

    // Hand tool
    [SerializeField]
    private float grabDistance = 5;

    private TreeLogGroup grabbedGroup;
    private Vector3 targetPosition;

    private void Start()
    {
        UpdateText();   
    }

    private void Update() 
    {
        if(grabbedGroup != null) {
            targetPosition = camera.transform.position + camera.transform.forward * grabDistance;
            grabbedGroup.transform.position = Vector3.Lerp(grabbedGroup.transform.position, targetPosition, Time.deltaTime * 5f);
        }
    }

    public void InputEvent(InputAction.CallbackContext context)
    {
        if(!context.performed/** && !context.canceled*/) 
            return;
        switch(context.action.name) {
            case "Primary":
                switch(toolbeltIndex) {
                    case 0:
                        PickupLog(context.performed);
                        break;
                    case 1:
                        if(context.performed)
                            SwingAxe();
                        break;
                }
                break;
            case "ChangeToolbelt":
                toolbeltIndex = (toolbeltIndex+1)%toolbeltOptions.Length;
                UpdateText();
                break;
        }
    }

    // InputPolling is toggled off
    public void InputPoll(InputAction action) 
    {

        switch(action.name) {
            case "Primary":
                switch(toolbeltIndex) {
                    case 0:
                        bool pressed = action.ReadValue<float>() > 0.1f;
                        // BLog.Highlight("Hand grab: " + pressed);
                        // test
                        break;
                }
                break;
        }
    }

    /// <summary>
    /// Given the player's camera pick a Log object
    /// </summary>
    /// <returns>null if the Raycast hit nothing or a non log GameObject, LogPickArgs if hit. </returns>
    private LogPickArgs PickLog() 
    {
        RaycastHit hit;
        Physics.Raycast(camera.transform.position, camera.transform.forward, out hit, 10f);
        if(hit.collider == null)
            return null;

        Vector3 hitPointLocal = hit.transform.InverseTransformPoint(hit.point);
        // We do this because we know that the TreeLogVisuals are a child of the TreeLog GameObject
        TreeLog log = hit.collider.transform.parent.gameObject.GetComponent<TreeLog>();

        if(log == null)
            return null;

        TreeLogGroup group = log.GetComponentInParent<TreeLogGroup>();

        if(group == null) {
            Debug.LogError($"Failed to find TreeLogGroup component in parent of game object \"{log.gameObject.name}\"");
            return null;
        }

        return new(hit, hitPointLocal, log, group);
    }

    private void SwingAxe() 
    {
        LogPickArgs args = PickLog();
        if(args == null)
            return;

        int[] identifierPath = args.log.GetIdentifierPath();
        args.group.HitLog(identifierPath, args.hit.point);
    }

    private void PickupLog(bool performed) 
    {
        if(performed) {
            LogPickArgs args = PickLog();
            if(args == null)
                return;

            grabbedGroup = args.group;
            grabbedGroup.GetComponent<Rigidbody>().isKinematic = true;
        } else {
            grabbedGroup.GetComponent<Rigidbody>().isKinematic = false;
            grabbedGroup = null;
        }
    }

    private void UpdateText() 
    {
        if(toolbeltText != null)
            toolbeltText.text = toolbeltOptions[toolbeltIndex];
    }

    /// <summary>
    /// Class for 
    /// </summary>
    private class LogPickArgs {
        public RaycastHit hit;
        public Vector3 hitPointLocal;
        public TreeLog log;
        public TreeLogGroup group;

        public LogPickArgs(RaycastHit hit, Vector3 hitPointLocal, TreeLog log, TreeLogGroup group)
        {
            this.hit = hit;
            this.hitPointLocal = hitPointLocal;
            this.log = log;
            this.group = group;
        }
    }

}