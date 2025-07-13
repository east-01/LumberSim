using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using EMullen.Core;
using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectableRenderer : MonoBehaviour 
{
    [Header("References")]
    [SerializeField]
    private MMF_Player mmfPlayer;
    [SerializeField]
    private TMP_Text nameText;
    [SerializeField]
    private TMP_Text descriptionText;

    [Header("Settings")]
    [SerializeField]
    private float heightNoDesc;
    [SerializeField]
    private float heightWithDesc;
    [SerializeField]
    private float descLineHeight;

    private Player player;
    private CanvasGroup canvasGroup;

    private Selectable shownSelectable;

    private void Awake()
    {
        player = GetComponentInParent<Player>();
        if(player == null) {
            Debug.LogError("Failed to get player in parent. It is assumed that the PlayerHUDMenuController is on a canvas that's a child of a Player GameObject.");
            return;
        }

        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Start()
    {
        Hide();    
    }

    private void Update() 
    {
        // BLog.Highlight("####");
        // BLog.Highlight($"selected \"{player.GrabbablePicker.SelectedGrabbable}\"");
        // if(player.GrabbablePicker.SelectedGrabbable != null) BLog.Highlight($"nobj: \"{player.GrabbablePicker.SelectedGrabbable.NetworkObject}\"");

        Selectable preferredSelectable = GetPreferredSelectable();
        if(shownSelectable != preferredSelectable) {
            shownSelectable = preferredSelectable;

            if(shownSelectable == null) {
                bool isShown = canvasGroup.alpha == 1f;
                Hide(isShown); 
            } else if(Render(shownSelectable)) {
                Show(true);
            }
        }
    }

    /// <summary>
    /// Gets the preferred selectable that is to be shown to the player.
    /// Priority:
    ///   Higher: Held Grabbable from HandsImpl
    ///   Lower: Current Selectable from RaycastPicker
    /// </summary>
    /// <returns>Preferred selectable</returns>
    private Selectable GetPreferredSelectable() 
    {
        HandsImpl hands = player.ToolBelt.GetImplementation(Item.NONE) as HandsImpl;
        if(hands.Grabbed != null && hands.Grabbed.TryGetComponent(out Selectable grabbedSelectable)) {
            return grabbedSelectable;
        } else if(player.RaycastPicker.CurrentSelectable != null) {
            return player.RaycastPicker.CurrentSelectable;
        } else
            return null;
    }

    public void Show(bool animate = false) 
    {
        mmfPlayer.StopFeedbacks();
        SetFeedbacksDirections(true);
        if(animate)
            mmfPlayer.PlayFeedbacks();
        else
            canvasGroup.alpha = 1f;
    }

    public void Hide(bool animate = false) 
    {
        mmfPlayer.StopFeedbacks();
        SetFeedbacksDirections(false);
        if(animate) {
            mmfPlayer.PlayFeedbacks();
        } else {
            canvasGroup.alpha = 0f;
        }
    }

    private void SetFeedbacksDirections(bool forward) 
    {
        foreach(var fb in mmfPlayer.FeedbacksList) {
            fb.Timing.PlayDirection = forward ? MMFeedbackTiming.PlayDirections.AlwaysNormal : MMFeedbackTiming.PlayDirections.AlwaysRewind;
        }
    }

    /// <summary>
    /// Render the selectable, returns true/false if the render is successful.
    /// </summary>
    /// <param name="selectable">The selectable to render, will access it's selectable render info.</param>
    /// <returns>True/false success status</returns>
    /// <exception cref="InvalidOperationException">The selectable is null.</exception>
    public bool Render(Selectable selectable) 
    {
        if(selectable == null)
            throw new InvalidOperationException("Null selectable.");

        if(!selectable.SelectableRenderInfoV.HasValue) {
            Debug.LogWarning("Skipping SelectableRenderer Render call since there is no render info.");
            return false;
        }

        SelectableRenderInfo info = selectable.SelectableRenderInfoV.Value;

        if(!info.renders)
            return false;

        UpdateTextElements(info);

        descriptionText.ForceMeshUpdate();
        LayoutRebuilder.ForceRebuildLayoutImmediate(descriptionText.rectTransform);
        // UpdateSize(info);

        return true;
    }

    private void UpdateTextElements(SelectableRenderInfo info) 
    {
        nameText.text = info.name;
        descriptionText.text = string.Join("\n", info.descriptionLines);
    }

    // private void UpdateSize(SelectableRenderInfo info) 
    // {
    //     RectTransform t = GetComponent<RectTransform>();
    //     Vector2 sizeDelta = t.sizeDelta;
    //     int descLineCnt = info.descriptionLines.Length;
    //     if(descLineCnt == 0)
    //         sizeDelta.y = heightNoDesc;
    //     else {
    //         sizeDelta.y = heightWithDesc + (descLineHeight * (descLineCnt-1));
    //     }
    //     t.sizeDelta = sizeDelta;            
    // }
}