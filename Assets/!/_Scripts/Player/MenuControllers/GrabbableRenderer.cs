using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using EMullen.Core;
using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;

public class GrabbableRenderer : MonoBehaviour 
{
    [SerializeField]
    private MMF_Player mmfPlayer;
    [SerializeField]
    private TMP_Text nameText;
    [SerializeField]
    private TMP_Text descriptionText;

    private Player player;
    private CanvasGroup canvasGroup;

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

    public void Show(bool animate = false) 
    {
        if(animate)
            mmfPlayer.PlayFeedbacks();
        else
            canvasGroup.alpha = 1f;
    }

    public void Hide(bool animate = false) 
    {
        if(animate) {
            mmfPlayer.PlayFeedbacksInReverse();
        } else {
            canvasGroup.alpha = 0f;
        }
    }

    public void Render(Grabbable grabbable) 
    {
        if(grabbable == null) {
            Hide(true);
            nameText.text = "";
            descriptionText.text = "";
            return;
        }

        Show(true);

        GrabbableInfo info = grabbable.Info;
        IGrabbable iGrabbable = grabbable.GetIGrabbable();

        GrabbableRenderArgs args = GrabbableRenderArgs.DefaultRenderArgs(info);
        if(iGrabbable != null) {
            GrabbableRenderArgs? renderResult = iGrabbable.Render(player.uid.Value.ToString());
            if(renderResult.HasValue) args = renderResult.Value;
        }

        nameText.text = args.name;
        descriptionText.text = string.Join("\n", args.descriptionLines);
    }
}

public struct GrabbableRenderArgs 
{
    public string name;
    public string[] descriptionLines;

    public GrabbableRenderArgs(string name, string[] descriptionLines) 
    {
        this.name = name;
        this.descriptionLines = descriptionLines;
    }

    public static GrabbableRenderArgs DefaultRenderArgs(GrabbableInfo input) { return new(input.DisplayName, input.DescriptionLines); }
}