using System;
using EMullen.Core;
using EMullen.Networking;
using EMullen.PlayerMgmt;
using EMullen.SceneMgmt;
using FishNet.Connection;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

/// <summary>
/// The player class is the top level controller for the Player prefab, it is activated by the
///   ConnectPlayer() call when the PlayerManager sends a LocalPlayer.
/// </summary>
public class Player : NetworkBehaviour, IS3
{    
    [Header("References")]
    [SerializeField]
    private new Camera camera;
    public Camera Camera => camera;
    [SerializeField]
    private AttachBehaviourController attachBehaviourController;
    public AttachBehaviourController AttachBehaviourController => attachBehaviourController;
    [SerializeField]
    private ProgressionManager progressionManager;
    public ProgressionManager ProgressionManager => progressionManager;
    [SerializeField]
    private PlayerTransactionManager playerTransactionManager;
    public PlayerTransactionManager PlayerTransactionManager => playerTransactionManager;
    [SerializeField]
    private NetworkedAudioController networkedAudioController;
    public NetworkedAudioController NetworkedAudioController => networkedAudioController;
    [SerializeField]
    private PlayerHUDMenuController playerHUDMenuController;
    public PlayerHUDMenuController PlayerHUD => playerHUDMenuController;
    [SerializeField]
    private PlayerInputManager playerInputManager;
    public PlayerInputManager PlayerInputManager => playerInputManager;
    [SerializeField]
    private GrabbablePicker grabbablePicker;
    public GrabbablePicker GrabbablePicker => grabbablePicker;
    [SerializeField]
    private ToolBelt toolBelt;
    public ToolBelt ToolBelt => toolBelt;

#if UNITY_EDITOR
    [Header("Readouts")]
    [SerializeField]
    private string uidReadout; // Here to show uid in editor
#endif

    // Cached references
    public CameraManager CameraManager { get; private set; }
    public GameplayManager GameplayManager { get; private set; }

    // Variables
    public readonly SyncVar<string> uid = new();

    public LocalPlayer LocalPlayer { get; private set; }
    public bool IsPaused { get; private set; }

    public bool HasPlayerData => PlayerDataRegistry.Instance != null && uid.Value != null && PlayerDataRegistry.Instance.Contains(uid.Value);
    public PlayerData PlayerData => PlayerDataRegistry.Instance.GetPlayerData(uid.Value);


#region Initializers
    private void Awake()
    {
        CameraManager = GetComponent<CameraManager>();
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

        if(Input.GetKeyDown(KeyCode.Escape)) {
            SetPaused(!IsPaused);
        }

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

    public void SetPaused(bool paused) 
    {
        IsPaused = paused;
        attachBehaviourController.UpdateAttachBehaviours();
    }

    /// <summary>
    /// Should the game "consume" the users mouse- i.e. will it be disabled or active.
    /// </summary>
    /// <param name="consume">Should the mouse be consumed.</param>
    public void ConsumeMouse(bool consume) 
    {
        Cursor.lockState = consume ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !consume;
    }

    public void ShowHUDWarning(string message, float time) 
    {
        if(LocalPlayer == null) {
            NetworkConnection targ = PlayerDataRegistry.Instance.GetPlayerData(uid.Value).GetData<NetworkIdentifierData>().GetNetworkConnection();
            TargetRPCShowHUDWarning(targ, message, time);
            return;
        }
        
        // Use pragma warning disable to hide deprecation warning. 
        // PlayerHUD.ShowWarning SHOULD be called from here.
#pragma warning disable 0618
        PlayerHUD.ShowWarning(message, time);
#pragma warning restore 0618
    }

    [TargetRpc]
    private void TargetRPCShowHUDWarning(NetworkConnection targ, string message, float time) => ShowHUDWarning(message, time);

}
