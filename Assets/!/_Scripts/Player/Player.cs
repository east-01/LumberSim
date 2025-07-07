using System;
using System.Transactions;
using EMullen.Core;
using EMullen.Networking;
using EMullen.PlayerMgmt;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Component.Transforming;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The player class is the top level controller for the Player prefab, it is activated by the
///   ConnectPlayer() call when the PlayerManager sends a LocalPlayer.
/// </summary>
[RequireComponent(typeof(PlayerInputManager))]
[RequireComponent(typeof(ToolBelt))]
[RequireComponent(typeof(AttachBehaviourController))]
[RequireComponent(typeof(CameraManager))]
public class Player : NetworkBehaviour, IS3
{
    public readonly SyncVar<string> uid = new();
#if UNITY_EDITOR
    [SerializeField]
    private string uidReadout; // Here to show uid in editor
#endif

    public bool HasPlayerData => PlayerDataRegistry.Instance != null && uid.Value != null && PlayerDataRegistry.Instance.Contains(uid.Value);
    public PlayerData PlayerData => PlayerDataRegistry.Instance.GetPlayerData(uid.Value);

    public GameplayManager GameplayManager { get; private set; }
    public LocalPlayer LocalPlayer { get; private set; }

    private AttachBehaviourController attachBehaviourController;
    private PlayerInputManager playerInputManager;

    [SerializeField]
    private new Camera camera;
    public Camera Camera => camera;
    public CameraManager CameraManager { get; private set; }
    public FirstPersonCamera FirstPersonCamera => Camera.GetComponent<FirstPersonCamera>();
    [SerializeField]
    private GrabbablePicker grabbablePicker;
    public GrabbablePicker GrabbablePicker => grabbablePicker;

    public bool IsPaused { get; private set; }

    public PlayerHUDMenuController GetHUD() => GetComponentInChildren<PlayerHUDMenuController>();
    public NetworkedAudioController GetNetworkedAudioController() => GetComponent<NetworkedAudioController>();
    public ToolBelt GetToolBelt() => GetComponent<ToolBelt>();
    
    [SerializeField]
    private PlayerTransactionManager playerTransactionManager;
    public PlayerTransactionManager PlayerTransactionManager => playerTransactionManager;

#region Initializers
    private void Awake()
    {
        CameraManager = GetComponent<CameraManager>();
        playerInputManager = GetComponent<PlayerInputManager>();
        attachBehaviourController = GetComponentInChildren<AttachBehaviourController>();
    }

    public void SingletonRegistered(Type type, object singleton)
    {
        if(type != typeof(GameplayManager))
            return;

        GameplayManager = singleton as GameplayManager;
    }

    public void SingletonDeregistered(Type type, object singleton)
    {
        if(type != typeof(GameplayManager))
            return;

    }
#endregion

    private void Update() 
    {
#if UNITY_EDITOR
        uidReadout = uid.Value;
#endif

        // Safely subscribe to the GameplayManager singleton
        if(gameObject.scene.name == "GameplayScene") {
            SceneLookupData lookupData = gameObject.scene.GetSceneLookupData();

            if(!SceneSingletons.IsSubscribed(this, lookupData, typeof(GameplayManager))) {
                SceneSingletons.SubscribeToSingleton(this, lookupData, typeof(GameplayManager));
            }
        }

        // Mute AudioListener if there's no player.
        bool localPlayerExists = LocalPlayer != null && LocalPlayer.Input != null;

        if(!localPlayerExists && gameObject.GetComponentInChildren<AudioListener>() != null) {
            gameObject.GetComponentInChildren<AudioListener>().gameObject.SetActive(false);
        }

        // if(Input.GetKeyDown(KeyCode.U) || Input.GetKeyDown(KeyCode.I)) {
        //     float delta = Input.GetKeyDown(KeyCode.U) ? 5 : -5;
        //     PlayerData pd = PlayerDataRegistry.Instance.GetPlayerData(uid.Value);
        //     pd.EnsureLumberData();
        //     GeneralPlayerData gpd = pd.GetData<GeneralPlayerData>();
        //     gpd.maxCarryWeight = gpd.maxCarryWeight + delta;
        //     pd.SetData(gpd);
        //     BLog.Highlight($"Set carry weight to: {gpd.maxCarryWeight}");
        // }
    }

    public void ConnectPlayer(string uuid, Player player) 
    {
        int? idx = PlayerManager.Instance.GetLocalIndex(uuid);
        if(idx.HasValue) {
            ConnectPlayer(PlayerManager.Instance.LocalPlayers[idx.Value]);
        } else {
            attachBehaviourController.UpdateAttachBehaviours();
            gameObject.name = "Player unattached";
        }
    }

    public void ConnectPlayer(LocalPlayer localPlayer) 
    {
        if(localPlayer.UID != uid.Value) {
            Debug.LogError($"Failed to connect Player to LocalPlayer, uuids mismatch. Stored on player: \"{uid.Value}\" Attempting to connect: \"{localPlayer.UID}\"");
            return;
        }

        LocalPlayer = localPlayer;
        GetComponent<PlayerInputManager>().ConnectPlayer(localPlayer.Input);       

        attachBehaviourController.UpdateAttachBehaviours();

        gameObject.name = $"Player (LocalPlayer {localPlayer.Input.playerIndex})";
    }

    public void ShowHUDWarning(string message, float time) 
    {
        if(LocalPlayer == null) {
            NetworkConnection targ = PlayerDataRegistry.Instance.GetPlayerData(uid.Value).GetData<NetworkIdentifierData>().GetNetworkConnection();
            TargetRPCShowHUDWarning(targ, message, time);
            return;
        }

        GetHUD().ShowWarning(message, time);
    }
    [TargetRpc]
    private void TargetRPCShowHUDWarning(NetworkConnection targ, string message, float time) => ShowHUDWarning(message, time);

}
