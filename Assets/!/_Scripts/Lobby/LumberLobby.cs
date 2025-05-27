using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using EMullen.Core;
using EMullen.Networking.Lobby;
using EMullen.SceneMgmt;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using UnityEngine.SceneManagement;

public class LumberLobby : GameLobby 
{
    public static readonly int WINS_PER_MAP = 3;
    public static readonly int REQUIRED_PLAYERS = 2;

    public GameplayManager GameplayManager { get; private set; }

    public SceneLookupData GameplayScene { get; private set; }

    public LumberLobby() : base() 
    {
        State = new StateLoadScene(this);
        GameplayScene = null;
    
        LobbyManager.Instance.LobbyUpdatedEvent += LobbyManager_LobbyUpdatedEvent;
    }

    ~LumberLobby()
    {
        LobbyManager.Instance.LobbyUpdatedEvent -= LobbyManager_LobbyUpdatedEvent;
    }

    public override void Update()
    {
        base.Update();

        if (GameplayScene is not null && GameplayManager == null)
            ConnectGameplayManager(GameplayScene);

        if (GameplayManager == null)
            return;
            
        
    }

    public override bool Joinable() => PlayerCount < REQUIRED_PLAYERS && GameplayScene != null;

    public override void ClaimedScene(SceneLookupData sceneLookupData)
    {
        base.ClaimedScene(sceneLookupData);

        GameplayScene = sceneLookupData;
        BLog.Highlight($"Set gameplay scene to {GameplayScene}");

        if(SceneSingletons.Contains(sceneLookupData, typeof(GameplayManager)))
            ConnectGameplayManager(sceneLookupData);
    }

    public override void UnclaimedScene(SceneLookupData sceneLookupData)
    {
        base.UnclaimedScene(sceneLookupData);

        if(sceneLookupData != GameplayScene)
            return;

        DisconnectGameplayManager(sceneLookupData);
        GameplayScene = null;
    }

    private void ConnectGameplayManager(SceneLookupData sld) 
    {
        if(GameplayScene is not null && GameplayScene != sld)
            throw new InvalidOperationException($"Can't connect gameplay manager to scene lookup data {sld} since we're currently waiting on {GameplayScene}");
        
        GameplayManager = SceneSingletons.Get(sld, typeof(GameplayManager)) as GameplayManager;
        GameplayManager.Lobby = this;
    }

    private void DisconnectGameplayManager(SceneLookupData sld) 
    {
        GameplayManager.Lobby = null;
        GameplayManager = null;
    }

    private void LobbyManager_LobbyUpdatedEvent(string lobbyID, LobbyData newData, LobbyUpdateReason reason)
    {
        if(reason == LobbyUpdateReason.PLAYER_JOIN && State.GetType() != typeof(StateLoadScene)) {

            IEnumerable<NetworkConnection> inLobby = Connections;

            Scene? mapSceneNullable = InstanceFinder.SceneManager.SceneConnections.Keys.Search(GameplayScene);
            if(!mapSceneNullable.HasValue)
                throw new InvalidOperationException("Can't add joined client to map scene, it wasn't found in search.");

            IEnumerable<NetworkConnection> inScene = InstanceFinder.SceneManager.SceneConnections[mapSceneNullable.Value];

            NetworkConnection[] connectionsToAdd = inLobby.Except(inScene).ToArray();

            SceneLoadData sld = new(GameplayScene);
            sld.ReplaceScenes = ReplaceOption.All;
            sld.PreferredActiveScene = new PreferredScene(GameplayScene);
            InstanceFinder.SceneManager.LoadConnectionScenes(connectionsToAdd.ToArray(), sld);
        }
    }

}