using EMullen.Networking.Lobby;

public class StateRunning : LobbyState
{
    public StateRunning(GameLobby gameLobby) : base(gameLobby) {}

    public override LobbyState CheckForStateChange()
    {
        return null;
    }
}