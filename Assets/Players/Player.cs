using System;
using EMullen.Core;
using EMullen.PlayerMgmt;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Component.Transforming;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : NetworkBehaviour, IS3
{
    public readonly SyncVar<string> uid = new();
    [SerializeField]
    private string uidReadout;

    private GameplayManager gameplayManager;

    private LocalPlayer localPlayer;

    [SerializeField]
    private GameObject playerCameraPrefab;

    [SerializeField]
    private new FirstPersonCamera camera;

    private void Start() 
    {
        // characterController = GetComponent<ExampleCharacterController>();
        // BLog.Highlight("Loaded character controller as: " + characterController);
    }

    public void SingletonRegistered(Type type, object singleton)
    {
        if(type != typeof(GameplayManager))
            return;

        gameplayManager = singleton as GameplayManager;
        gameplayManager.GetComponent<PlayerObjectManager>().PlayerConnectedEvent += PlayerObjectManager_PlayerConnectedEvent;
        BLog.Highlight("Registered Gameplaymanager");
    }

    public void SingletonDeregistered(Type type, object singleton)
    {
        if(type != typeof(GameplayManager))
            return;

        gameplayManager.GetComponent<PlayerObjectManager>().PlayerConnectedEvent -= PlayerObjectManager_PlayerConnectedEvent;
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

        uidReadout = uid.Value;

        // if(localPlayer != null && camera == null && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "GameplayScene") {
        //     GameObject cameraObject = Instantiate(playerCameraPrefab, transform);
        //     camera = cameraObject.GetComponent<FirstPersonCamera>();
        // }

        if(localPlayer != null && localPlayer.Input != null)
            HandleInput();
        else if(gameObject.GetComponentInChildren<AudioListener>() != null) {
            gameObject.GetComponentInChildren<AudioListener>().gameObject.SetActive(false);
        }
    }

    private void PlayerObjectManager_PlayerConnectedEvent(string uuid, Player player)
    {
        if(uuid != uid.Value)
            return;

        ConnectPlayer(uuid, player);
    }

    public void ConnectPlayer(string uuid, Player player) 
    {
        int? idx = PlayerManager.Instance.GetLocalIndex(uuid);
        if(!idx.HasValue) {
            Debug.LogError("Failed to connect player locally, couldn't resolve index.");
            return;
        }

        ConnectPlayer(PlayerManager.Instance.LocalPlayers[idx.Value]);
    }

    public void ConnectPlayer(LocalPlayer localPlayer) 
    {
        if(localPlayer.UID != uid.Value) {
            Debug.LogError($"Failed to connect Player to LocalPlayer, uuids mismatch. Stored on player: \"{uid.Value}\" Attempting to connect: \"{localPlayer.UID}\"");
            return;
        }

        this.localPlayer = localPlayer;
        GetComponent<PlayerMovement>().input = this.localPlayer.Input;

        this.localPlayer.Input.onActionTriggered += PlayerControls_OnActionTriggered;
        BLog.Highlight("Connected to local player idx " + this.localPlayer.Input.playerIndex);
    }

    private void PlayerControls_OnActionTriggered(InputAction.CallbackContext context)
    {
        if(!context.performed/** && !context.canceled*/) 
            return;
        switch(context.action.name) {
            case "Primary":
                SwingAxe();
                break;
        }
    }

    private void HandleInput() 
    {
        PlayerInput input = localPlayer.Input;

        Vector2 cameraInput;
        if(input.currentControlScheme == "KeyboardMouse") {
            cameraInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        } else {
            cameraInput = input.actions["Look"].ReadValue<Vector2>();
        }

        camera.input = cameraInput;
    }

    private void SwingAxe() 
    {
        RaycastHit hit;
        Physics.Raycast(camera.transform.position, camera.transform.forward, out hit, 10f);
        if(hit.collider == null)
            return;

        Vector3 hitPointLocal = hit.transform.InverseTransformPoint(hit.point);
        // We do this because we know that the TreeLogVisuals are a child of the TreeLog GameObject
        TreeLog log = hit.collider.transform.parent.gameObject.GetComponent<TreeLog>();

        if(log == null)
            return;

        TreeLogGroup group = log.GetComponentInParent<TreeLogGroup>();

        if(group == null) {
            Debug.LogError($"Failed to find TreeLogGroup component in parent of game object \"{log.gameObject.name}\"");
            return;
        }

        // TODO: Play animations
        log.Hit(hitPointLocal);

        // List<int> childIndices = new();
        // TreeLog focusLog = log;
        // while(focusLog.Parent != null) {
        //     childIndices.Add(focusLog.transform.GetSiblingIndex());
        //     focusLog = focusLog.Parent;
        // }

        // Split log in half
        HitLog(group.GetComponent<NetworkObject>(), log.GetIdentifierPath(), hit.point);
    }

    // See TreeLog#GetIdentifierPath() for details on treeLogIP
    private void HitLog(NetworkObject choppableTreeObject, int[] treeLogIP, Vector3 hitGlobal) 
    {
        if(!InstanceFinder.IsServerStarted) {
            ServerRpcHitLog(choppableTreeObject, treeLogIP, hitGlobal);
            return;
        }

        GameObject obj = choppableTreeObject.gameObject;
        
        if(!obj.TryGetComponent(out TreeLogGroup group)) {
            Debug.LogError("Failed to get the TreeLogGroup for the hit NetworkObject!");
            return;
        }

        TreeLog hitLog = group.GetTreeLog(treeLogIP);
        if(hitLog == null) {
            Debug.LogError($"Failed to resolve hit log from treelog ip: {string.Join(", ", treeLogIP)}");
            return;
        }

        Debug.DrawLine(hitGlobal, hitLog.EndpointBackward, Color.red, 60);
        Debug.DrawLine(hitLog.EndpointForward, hitLog.EndpointBackward, Color.yellow, 60);

        // Store log length for finding the new logs length
        float origLogLength = hitLog.Data.length;

        // Calculate the length of the log still attached
        Vector3 hitPointVector = hitGlobal - hitLog.EndpointBackward;
        Vector3 logVector = hitLog.EndpointForward-hitLog.EndpointBackward;
        float attachedLength = Vector3.Dot(hitPointVector, logVector.normalized);

        // Create the data for the log that is still attached to the group
        TreeLogData newAttachedData = hitLog.Data.Clone();
        newAttachedData.length = attachedLength;
        newAttachedData.children = new TreeLogData[0];

        // Insert newly created data into the original groups data tree
        TreeLogData focusData = group.RootData;
        if(treeLogIP.Length > 0) {
            for(int branchLayer = 0; branchLayer < treeLogIP.Length; branchLayer++) {
                if(branchLayer < treeLogIP.Length - 1) {
                    // For all branches except the one we're targeting, iterate through the tree
                    focusData = focusData.children[treeLogIP[branchLayer]];
                } else {
                    // For the parent of the branch we're targeting, set the child to the new attached data
                    focusData.children[treeLogIP[branchLayer]] = newAttachedData;
                }
            }
            group.DataUpdated();
        } else {
            group.SetRootData(newAttachedData);
        }

        // Create the data for the new log group to be spawned
        float newLogLength = origLogLength-attachedLength;
        TreeLogData newLogData = hitLog.Data.Clone();
        newLogData.length = newLogLength;
        newLogData.children = (TreeLogData[]) hitLog.Data.children.Clone();

        // Spawn new log group
        gameplayManager.SpawnLogObject(hitGlobal, group.transform.rotation, newLogData);

    }

    [ServerRpc(RequireOwnership = false)]
    private void ServerRpcHitLog(NetworkObject choppableTreeObject, int[] treeLogIP, Vector3 hitGlobal) => HitLog(choppableTreeObject, treeLogIP, hitGlobal);

}
