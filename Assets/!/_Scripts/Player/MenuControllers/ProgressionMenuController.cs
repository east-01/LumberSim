using System.Collections;
using System.Collections.Generic;
using EMullen.Core;
using EMullen.MenuController;
using EMullen.PlayerMgmt;
using UnityEngine;

public class ProgressionMenuController : MenuController
{
    [SerializeField]
    private Transform draggableContainer;

    private Player player;
    private LocalPlayer lastFocusedPlayer;

    private Dictionary<string, ProgressionPointRenderer> cachedRenderers;
    private string selectedRenderer;

    protected new void Awake()
    {
        base.Awake();

        player = GetComponentInParent<Player>();
        if(player == null) {
            Debug.LogError("Failed to get player in parent. It is assumed that the PlayerHUDMenuController is on a canvas that's a child of a Player GameObject.");
            return;
        }
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D)) {

        }

        if(FocusedPlayer != lastFocusedPlayer) {
            UpdateProgressionPointRenderers();
            lastFocusedPlayer = FocusedPlayer;
        }   
    }

    protected override void Opened()
    {
        base.Opened();

        player.ProgressionManager.UpdateProgressionResults();
    }

    public void UpdateProgressionPointRenderers() 
    {
        if(FocusedPlayer == null)
            return;

        cachedRenderers = new();

        PlayerData pd = player.PlayerData;
        pd.EnsureLumberData();

        ProgressionData progression = pd.GetData<ProgressionData>();
        ProgressionTree.EvaluationResults evalResults = player.ProgressionManager.ProgressionResults.Value;

        for(int childIdx = 0; childIdx < draggableContainer.childCount; childIdx++) {
            GameObject progPointObj = draggableContainer.GetChild(childIdx).gameObject;
            ProgressionPointRenderer progPointRenderer = progPointObj.GetComponent<ProgressionPointRenderer>();
            string progPointID = progPointRenderer.Point.progressionID;

            progPointRenderer.shownProgressionData = progression;

            cachedRenderers.Add(progPointID, progPointRenderer);

            if(progression.HasUnlocked(progPointID))
                progPointRenderer.displayMode = ProgressionPointRenderer.DisplayMode.UNLOCKED;
            else if(evalResults.nextSteps != null && evalResults.nextSteps.ContainsKey(progPointID))
                progPointRenderer.displayMode = ProgressionPointRenderer.DisplayMode.NEXT_STEP; 
            else if(evalResults.canUnlock != null && evalResults.canUnlock.Contains(progPointID))
                progPointRenderer.displayMode = ProgressionPointRenderer.DisplayMode.CAN_UNLOCK;            
            else
                progPointRenderer.displayMode = ProgressionPointRenderer.DisplayMode.INVISIBLE;    
        }
    }

    public void MoveSelectedRenderer(int x, int y) 
    {

    }

    public void UpdateSelectedRenderer() 
    {
        if(selectedRenderer == null)
            selectedRenderer = player.ProgressionManager.ProgressionTree.Entries[0];
    }
}