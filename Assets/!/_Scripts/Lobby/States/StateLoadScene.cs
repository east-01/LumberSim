using System.Collections.Generic;
using System.Linq;
using EMullen.Networking.Lobby;
using EMullen.PlayerMgmt;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;

/// <summary>
/// StateLoadScene is for when the lobby is loading a map scene.
/// Transitions to StateWarmup once the scene is loaded for the server and clients, and all of the
///   players are spawned.
/// </summary>
public class StateLoadScene : LobbyState
{
    private bool toldPlayers = false;

    public StateLoadScene(GameLobby gameLobby) : base(gameLobby) 
    {
        SceneLoadData sld = new SceneLoadData(new SceneLookupData("GameplayScene"));
        sld.ReplaceScenes = ReplaceOption.All;

        InstanceFinder.SceneManager.LoadConnectionScenes(sld);
    }

    public override LobbyState CheckForStateChange()
    {
        LumberLobby lobby = gameLobby as LumberLobby;

        if(lobby.GameplayManager == null)
            return null;
        
        if(lobby.GameplayScene == null) {
            return null;
        }

        if(!toldPlayers) {
            foreach(NetworkConnection conn in lobby.Connections) {
                SceneLoadData sld = new SceneLoadData(lobby.GameplayScene);
                sld.ReplaceScenes = ReplaceOption.All;
                InstanceFinder.SceneManager.LoadConnectionScenes(conn, sld);
            }

            toldPlayers = true;   
        }

        PlayerObjectManager pom = lobby.GameplayManager.GetComponent<PlayerObjectManager>();
        List<string> players = lobby.Players.ToList();
        bool allPlayersSpawned = players.All(uid => pom.GetPlayer(uid) != null);

        if(!allPlayersSpawned)
            return null;

        return new StateRunning(gameLobby);
    }

}