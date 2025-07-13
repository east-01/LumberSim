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
    private RectTransform viewport;
    [SerializeField]
    private Transform draggableContainer;
    [SerializeField]
    private TMP_Text cashCountText;

    [Header("Settings")]
    [SerializeField]
    private float minScale = 0.65f;
    [SerializeField]
    private float maxScale = 1.35f;

    private Player player;
    private LocalPlayer lastFocusedPlayer;

    private Dictionary<string, ProgressionPointRenderer> cachedRenderers;
    private string selectedRenderer;
    private bool waitingOnEvalResults = false;

    private Vector4 borderBoundaries_LRTB;
    private float ScaleX => draggableContainer.transform.localScale.x;
    private float ScaleY => draggableContainer.transform.localScale.y;

    public ProgressionTree.EvaluationResults EvalResults => player.ProgressionManager.ProgressionResults.Value;
    public bool HasEvalResults => EvalResults.canUnlock != null;

    protected new void Awake()
    {
        base.Awake();

        player = GetComponentInParent<Player>();
        if(player == null) {
            Debug.LogError("Failed to get player in parent. It is assumed that the PlayerHUDMenuController is on a canvas that's a child of a Player GameObject.");
            return;
        }

        PlayerDataRegistry.Instance.PlayerDataUpdatedEvent += PlayerDataRegistry_PlayerDataUpdated;        
        player.ProgressionManager.ProgressionResults.OnChange += ProgressionResults_OnChange;
    }

    protected new void OnDestroy()
    {
        base.OnDestroy();   
        PlayerDataRegistry.Instance.PlayerDataUpdatedEvent -= PlayerDataRegistry_PlayerDataUpdated;
        player.ProgressionManager.ProgressionResults.OnChange -= ProgressionResults_OnChange;
    }

    private void PlayerDataRegistry_PlayerDataUpdated(PlayerData playerData, PlayerDataClass newData)
    {
        List<Type> whitelistedTypes = new() { typeof(ProgressionData), typeof(GeneralPlayerData) };
        if(playerData.GetUID() == player.uid.Value && whitelistedTypes.Contains(newData.GetType()))
            UpdateProgressionPointRenderers();
    }

    private void ProgressionResults_OnChange(ProgressionTree.EvaluationResults prev, ProgressionTree.EvaluationResults next, bool asServer)
    {
        // This is for client rendering, we don't need the asServer's call.
        if(asServer)
            return;

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

        if(waitingOnEvalResults && HasEvalResults) {
            waitingOnEvalResults = false;
            UpdateEvalResultsRequired();
        }
    }

    protected override void Opened()
    {
        base.Opened();

        player.ProgressionManager.UpdateProgressionResults();
        
        if(HasEvalResults)
            UpdateEvalResultsRequired();
        else
            waitingOnEvalResults = true;

        ComputeBoundaries();

        player.SetPaused(true);
        player.ConsumeMouse(false);
    }

    public void UpdateEvalResultsRequired() 
    {
        ProgressionTree.EvaluationResults evalResults = player.ProgressionManager.ProgressionResults.Value;

        UpdateProgressionPointRenderers();

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
        SetScale(1.15f);
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

                SetScale(ScaleX + 0.1f*dir);
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

        // BLog.Highlight($"Want to move to: {xPos}, {yPos}");

        Vector2 want = new(xPos, yPos);

        if(xPos < borderBoundaries_LRTB.x*ScaleX)
            xPos = borderBoundaries_LRTB.x*ScaleX;

        if(xPos > borderBoundaries_LRTB.y*ScaleX)
            xPos = borderBoundaries_LRTB.y*ScaleX;

        if(yPos < borderBoundaries_LRTB.w*ScaleY)
            yPos = borderBoundaries_LRTB.w*ScaleY;

        if(yPos > borderBoundaries_LRTB.z*ScaleY)
            yPos = borderBoundaries_LRTB.z*ScaleY;

        if(want != new Vector2(xPos, yPos))
            BLog.Highlight($"Will move to: {xPos}, {yPos}, lborder: {borderBoundaries_LRTB.x*ScaleX}, rborder: {borderBoundaries_LRTB.y*ScaleX}, tborder: {borderBoundaries_LRTB.z*ScaleY}, bborder: {borderBoundaries_LRTB.w*ScaleY}");

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

    /// <summary>
    /// Find the coordinates of the furthest left, right, top, and bottom progression points.
    /// </summary>
    private void ComputeBoundaries() 
    {
        Vector4 bounds = borderBoundaries_LRTB;
        BLog.Highlight($"Computing bounds {cachedRenderers.Values.Count}");
        foreach(ProgressionPointRenderer renderer in cachedRenderers.Values) {
            Rect rect = (renderer.transform as RectTransform).rect;
            BLog.Highlight($"lborder: {rect.xMin}, rborder: {rect.xMax}, tborder: {rect.yMax}, bborder: {rect.yMin}");
            bounds.x = Mathf.Min(bounds.x, rect.xMin);
            bounds.y = Mathf.Max(bounds.y, rect.xMax);
            bounds.z = Mathf.Max(bounds.z, rect.yMax);
            bounds.w = Mathf.Min(bounds.w, rect.yMin);
        }
        borderBoundaries_LRTB = bounds;
    }
}