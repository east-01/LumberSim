using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EMullen.Core;
using EMullen.MenuController;
using EMullen.PlayerMgmt;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenuController : MenuController 
{

    private Player player;

    protected new void Awake()
    {
        base.Awake();

        player = GetComponentInParent<Player>();
        if(player == null) {
            Debug.LogError("Failed to get player in parent. It is assumed that the PlayerHUDMenuController is on a canvas that's a child of a Player GameObject.");
            return;
        }
    }

    protected override void Opened()
    {
        base.Opened();

        player.SetPaused(true);
        player.ConsumeMouse(false);
    }
}