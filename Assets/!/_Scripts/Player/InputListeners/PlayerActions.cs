using EMullen.Core;
using EMullen.PlayerMgmt;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The player actions input listener class is a catch all for all extra player actions that don't
///   group well. Currently holds the axe upgrade action.
/// </summary>
[RequireComponent(typeof(Player))]
[RequireComponent(typeof(ToolBelt))]
[RequireComponent(typeof(NetworkedAudioController))]
public class PlayerActions : MonoBehaviour, IInputListener
{
    private Player player;
    private ToolBelt toolBelt;
    private NetworkedAudioController audioController;

    private void Awake()
    {
        player = GetComponent<Player>();
        toolBelt = GetComponent<ToolBelt>();
        audioController = GetComponent<NetworkedAudioController>();
    }

    public void InputEvent(InputAction.CallbackContext context)
    {
        switch(context.action.name) {
            case "Upgrade":
                if(!context.performed)
                    break;

                PlayerData pd = player.PlayerData;
                if(!pd.HasData<GeneralPlayerData>())
                    break;

                // Ensure there is a level to upgrade to
                GeneralPlayerData gpd = pd.GetData<GeneralPlayerData>();
                if(gpd.axeLevel > toolBelt.AxeStatsArr.Length-1)
                    break;

                // Get the target price and check if the player can afford
                float targPrice = toolBelt.AxeStatsArr[gpd.axeLevel+1].price;
                if(gpd.balance >= targPrice) {
                    gpd.balance -= targPrice;
                    gpd.axeLevel += 1;
                    pd.SetData(gpd);

                    audioController.PlaySound("bing");
                }

                break;
        }
    }

    public void InputPoll(InputAction action) {}

}
