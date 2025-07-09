using System;
using System.Collections;
using System.Collections.Generic;
using EMullen.Core;
using EMullen.MenuController;
using EMullen.PlayerMgmt;
using TMPro;
using UnityEngine;

/// <summary>
/// The PlayerHUDMenuController class is responsible for controlling everything in the player's
///   pov, shows warning messages, balance, and tool belt.
/// </summary>
public class PlayerHUDMenuController : MenuController
{

    public static readonly string SUBMENU_IN_GAME = "InGame";
    public static readonly string SUBMENU_PROGRESSION = "Progression";

    [Header("References")]
    [SerializeField]
    private TMP_Text warningText;
    [SerializeField]
    private InGameMenuController inGameMenuController;
    public InGameMenuController InGameMenuController => inGameMenuController;
    [SerializeField]
    private ProgressionMenuController progressionMenuController;
    public ProgressionMenuController ProgressionMenuController => progressionMenuController;

    // Variables
    private float warningHideTime;

    private void Update()
    {
        // ----- Warning text -----
        warningText.gameObject.SetActive(Time.time < warningHideTime);
    }

    /// <summary>
    /// Flash a warning message on the screen for a specific amount of time.
    /// </summary>
    /// <param name="message">The warning message to show.</param>
    /// <param name="time">The time in seconds for the message to persist.</param>
    [Obsolete("Use Player#ShowHUDWarning to network this call.")]
    public void ShowWarning(string message, float time) 
    {
        warningText.text = message;
        warningText.gameObject.SetActive(true);
        warningHideTime = Time.time+time;
    }
}
