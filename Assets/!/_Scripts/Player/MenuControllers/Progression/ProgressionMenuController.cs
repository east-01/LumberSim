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

public class ProgressionMenuController : MenuController
{
    [Header("References")]
    [SerializeField]
    private Transform draggableContainer;
    [SerializeField]
    private TMP_Text cashCountText;

    [Header("Settings")]
    [SerializeField]
    private Vector4 borderBoundaries_LRTB;
    [SerializeField]
    private float minScale = 0.65f;
    [SerializeField]
    private float maxScale = 1.35f;

    private Player player;
    private LocalPlayer lastFocusedPlayer;

    private Dictionary<string, ProgressionPointRenderer> cachedRenderers;
    private string selectedRenderer;

    private float Scale => draggableContainer.transform.localScale.x;
    
    protected new void Awake()
    {
        base.Awake();

        player = GetComponentInParent<Player>();
        if(player == null) {
            Debug.LogError("Failed to get player in parent. It is assumed that the PlayerHUDMenuController is on a canvas that's a child of a Player GameObject.");
            return;
        }

        PlayerDataRegistry.Instance.PlayerDataUpdatedEvent += PlayerDataRegistry_PlayerDataUpdated;        
    }

    protected new void OnDestroy()
    {
        base.OnDestroy();   
        PlayerDataRegistry.Instance.PlayerDataUpdatedEvent -= PlayerDataRegistry_PlayerDataUpdated;
    }

    private void PlayerDataRegistry_PlayerDataUpdated(PlayerData playerData, PlayerDataClass newData)
    {
        List<Type> whitelistedTypes = new() { typeof(ProgressionData), typeof(GeneralPlayerData) };
        if(playerData.GetUID() == player.uid.Value && whitelistedTypes.Contains(newData.GetType()))
            UpdateProgressionPointRenderers();
    }

    private void Update()
    {
        PlayerData pd = player.PlayerData;
        pd.EnsureLumberData();
        
        GeneralPlayerData gpd = pd.GetData<GeneralPlayerData>();
        cashCountText.text = "$" + gpd.balance.ToString("F2");

        if(FocusedPlayer != lastFocusedPlayer) {
            UpdateProgressionPointRenderers();
            lastFocusedPlayer = FocusedPlayer;
        }   
    }

    protected override void Opened()
    {
        base.Opened();

        player.ProgressionManager.UpdateProgressionResults();

        UpdateProgressionPointRenderers();

        ProgressionTree.EvaluationResults evalResults = player.ProgressionManager.ProgressionResults.Value;
        // string currentProgression = ;

        string moveToRenderer = "";
        if(evalResults.canUnlock.Count > 0) {
            moveToRenderer = evalResults.canUnlock[0];
        } else if(evalResults.nextSteps.Count > 0) {
            moveToRenderer = evalResults.nextSteps.Keys.ToArray()[0];
        }

        // Adjust scale and position of menu
        if(moveToRenderer != null && moveToRenderer.Length > 0) {
            ProgressionPointRenderer renderer = cachedRenderers[moveToRenderer];
            RectTransform rendTransform = renderer.transform as RectTransform;
            MoveTo(-rendTransform.localPosition.x, -rendTransform.localPosition.y);
        } else {
            MoveTo(0, 0);
        }
        SetScale(1.35f);
        
        player.SetPaused(true);
        player.ConsumeMouse(false);
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

            if(!progPointObj.TryGetComponent(out ProgressionPointRenderer progPointRenderer))
                continue;
            
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

    public void ProgPointRendererClicked(ProgressionPointRenderer renderer) 
    {
        ProgressionPoint point = renderer.Point;
        if(point == null)
            throw new InvalidOperationException("Clicked progression point renderer with no progression point set.");

        player.ProgressionManager.UnlockProgressionPoint(point.progressionID);
    }

    private bool trackMouse;

    protected override void Child_PlayerInput_ActionTriggered(InputAction.CallbackContext context)
    {
        base.Child_PlayerInput_ActionTriggered(context);

        switch(context.action.name) {
            case "ChangeToolbelt":
                if(!context.performed)
                    return;

                int dir = (int)Mathf.Sign(context.ReadValue<float>());

                SetScale(Scale + 0.1f*dir);
                break;
            case "Primary":
                // TODO: Perform raycast to check if dragging background or other element

                trackMouse = context.performed;
                break;
            case "Look":
                if(!trackMouse)
                    return;

                Vector2 value = context.ReadValue<Vector2>();
                RectTransform rt = draggableContainer.transform as RectTransform;

                Vector3 pos = rt.anchoredPosition;
                MoveTo(pos.x + value.x, pos.y + value.y);
                break;
        }
    }

    public void MoveTo(float xPos, float yPos) 
    {
        RectTransform rt = draggableContainer.transform as RectTransform;
        Vector3 pos = rt.anchoredPosition;

        if(xPos < borderBoundaries_LRTB.x)
            xPos = borderBoundaries_LRTB.x;

        if(xPos > borderBoundaries_LRTB.y)
            xPos = borderBoundaries_LRTB.y;

        if(yPos < borderBoundaries_LRTB.w)
            yPos = borderBoundaries_LRTB.w;

        if(yPos > borderBoundaries_LRTB.z)
            yPos = borderBoundaries_LRTB.z;

        pos.x = xPos;
        pos.y = yPos;
        rt.anchoredPosition = pos;
    }

    public void SetScale(float scale) 
    {
        if(scale > maxScale)
            scale = maxScale;

        if(scale < minScale)
            scale = minScale;

        Vector3 scaleVec = Vector3.zero;
        scaleVec.x += scale;
        scaleVec.y += scale;
        draggableContainer.transform.localScale = scaleVec;
    }

    public void UpdateSelectedRenderer() 
    {
        if(selectedRenderer == null)
            selectedRenderer = player.ProgressionManager.ProgressionTree.Entries[0];
    }
}