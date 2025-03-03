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

[RequireComponent(typeof(PlayerInputManager))]
public class Player : NetworkBehaviour, IS3
{
    public readonly SyncVar<string> uid = new();
#if UNITY_EDITOR
    [SerializeField]
    private string uidReadout; // Here to show uid in editor
#endif

    private GameplayManager gameplayManager;
    private LocalPlayer localPlayer;

    private PlayerInputManager playerInputManager;

    [SerializeField]
    private new Camera camera;

#region Initializers
    private void Awake()
    {
        playerInputManager = GetComponent<PlayerInputManager>();
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
#endregion

    private void Update() 
    {
        uidReadout = uid.Value;

        // Safely subscribe to the GameplayManager singleton
        if(gameObject.scene.name == "GameplayScene") {
            SceneLookupData lookupData = gameObject.scene.GetSceneLookupData();

            if(!SceneSingletons.IsSubscribed(this, lookupData, typeof(GameplayManager))) {
                SceneSingletons.SubscribeToSingleton(this, lookupData, typeof(GameplayManager));
            }
        }

        // Mute AudioListener if there's no player.
        bool localPlayerExists = localPlayer != null && localPlayer.Input != null;

        if(!localPlayerExists && gameObject.GetComponentInChildren<AudioListener>() != null) {
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
        GetComponent<PlayerInputManager>().ConnectPlayer(localPlayer.Input);        

        BLog.Highlight("Connected to local player idx " + this.localPlayer.Input.playerIndex);
    }
}
