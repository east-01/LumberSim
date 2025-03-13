using System;
using EMullen.Core;
using EMullen.PlayerMgmt;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Component.Transforming;
using FishNet.Managing.Scened;
using FishNet.Object;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(NetworkedAudioController))]
public class ToolBelt : NetworkBehaviour, IInputListener, IS3
{

    [SerializeField]
    private new Camera camera;
    [SerializeField]
    private TMP_Text toolbeltText;

    private NetworkedAudioController audioController;
    private GameplayManager gameplayManager;
    int toolbeltIndex = 0;
    string[] toolbeltOptions = new string[] {"hands", "axe"};

    // Hand tool
    [SerializeField]
    private float grabDistance = 5;
    [SerializeField]
    private float maxCarryWeight = 60;

    private TreeLogGroup grabbedGroup;
    private Vector3 grabOffset;
    private Vector3 targetPosition;

    private void Awake()
    {
        audioController = GetComponent<NetworkedAudioController>();
    }

    private void Start()
    {
        UpdateText();   
    }

    public void SingletonRegistered(Type type, object singleton)
    {
        if(type != typeof(GameplayManager))
            return;

        gameplayManager = singleton as GameplayManager;
    }

    public void SingletonDeregistered(Type type, object singleton)
    {
        if(type != typeof(GameplayManager))
            return;

        gameplayManager = null;
    }

    private void Update() 
    {
        // Safely subscribe to the GameplayManager singleton
        if(gameObject.scene.name == "GameplayScene") {
            SceneLookupData lookupData = gameObject.scene.GetSceneLookupData();

            if(!SceneSingletons.IsSubscribed(this, lookupData, typeof(GameplayManager))) {
                SceneSingletons.SubscribeToSingleton(this, lookupData, typeof(GameplayManager));
            }
        }
        
        if(grabbedGroup != null) {
            targetPosition = grabOffset + (camera.transform.position + camera.transform.forward * grabDistance);
            UpdateGrabbedGroupPosition(grabbedGroup.NetworkObject, targetPosition);
        }
    }

    public void UpdateGrabbedGroupPosition(NetworkObject grabbedGroup, Vector3 targetPosition) 
    {
        if(!InstanceFinder.IsServerStarted) {
            grabbedGroup.transform.position = Vector3.Lerp(grabbedGroup.transform.position, targetPosition, Vector3.Distance(grabbedGroup.transform.position, targetPosition)/3f);
            ServerRpcUpdateGrabbedGroupPosition(grabbedGroup, targetPosition);
            return;
        }

        grabbedGroup.transform.position = Vector3.Lerp(grabbedGroup.transform.position, targetPosition, Vector3.Distance(grabbedGroup.transform.position, targetPosition)/3f);
    }
    [ServerRpc(RequireOwnership = false)]
    public void ServerRpcUpdateGrabbedGroupPosition(NetworkObject grabbedGroup, Vector3 targetPosition) 
    {

        // if (InstanceFinder.NetworkManager..Spawn().SpawnedObjects.TryGetValue(objectId, out NetworkObject grabbedGroup))
        // {
            UpdateGrabbedGroupPosition(grabbedGroup, targetPosition);
        // }
    }

    public void InputEvent(InputAction.CallbackContext context)
    {
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
        audioController.PlaySound("swingaxe");

        LogPickArgs args = PickLog();
        if(args == null)
            return;

        int[] identifierPath = args.log.GetIdentifierPath();
        args.group.HitLog(identifierPath, args.hit.point, LocalConnection);
    }

    private void PickupLog(bool performed) 
    {
        if(performed) {
            LogPickArgs args = PickLog();
            if(args == null)
                return;
            // Only logs with rigidbodies (affected by physics) should be grabbable
            if(!args.group.TryGetComponent(out Rigidbody grabbedRB))
                return;

            float weight = args.group.EvaluateTotalWeight();
            if(weight > maxCarryWeight) {
                PlayerHUDMenuController mc = gameObject.GetComponentInChildren<PlayerHUDMenuController>();
                if(mc != null)
                    mc.ShowWarning("Too heavy!", 0.75f);
                grabbedRB.AddForce(Vector3.up*3f);
                return;
            }

            audioController.PlaySound("pickup");

            grabbedRB.useGravity = false;

            grabbedGroup = args.group;
            grabOffset = /*args.hit.point-args.group.transform.position*/Vector3.zero;

            ServerSetPickedUp(args.group.ObjectId, performed);
        } else {
            if(grabbedGroup != null && grabbedGroup.TryGetComponent(out Rigidbody grabbedRB))
                grabbedRB.useGravity = true;

            if(grabbedGroup != null)
                ServerSetPickedUp(grabbedGroup.ObjectId, performed);

            grabbedGroup = null;
            grabOffset = Vector3.zero;
        }
    }

    private void ServerSetPickedUp(int logNetID, bool performed) 
    {
        if(!InstanceFinder.IsServerStarted) {
            ServerRpcServerSetPickedUp(logNetID, performed);
            return;
        }

        TreeLogGroup group = gameplayManager.GetLogGroupFromNetworkID(logNetID);
        Rigidbody rb = group.GetComponent<Rigidbody>();
        rb.useGravity = !performed;

    }
    [ServerRpc(RequireOwnership = false)]
    private void ServerRpcServerSetPickedUp(int logNetID, bool performed) => ServerSetPickedUp(logNetID, performed);

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