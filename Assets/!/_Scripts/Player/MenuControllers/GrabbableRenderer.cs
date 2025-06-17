using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using EMullen.Core;
using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;

public class GrabbableRenderer : MonoBehaviour 
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

    private Grabbable shownGrabbable;

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

        if(shownGrabbable != player.GrabbablePicker.SelectedGrabbable) {
            shownGrabbable = player.GrabbablePicker.SelectedGrabbable;
            Render(shownGrabbable);

            if(shownGrabbable == null)
                Hide(true);
            else
                Show(true);
        }
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

    public void Render(Grabbable grabbable) 
    {
        GrabbableRenderArgs renderArgs = GetRenderArgs(grabbable);

        UpdateTextElements(renderArgs);
        UpdateSize(renderArgs);
    }

    private GrabbableRenderArgs GetRenderArgs(Grabbable grabbable) 
    {
        if(grabbable == null)
            return GrabbableRenderArgs.CreateEmpty();

        GrabbableInfo info = grabbable.Info;
        IGrabbable iGrabbable = grabbable.GetIGrabbable();

        GrabbableRenderArgs args = GrabbableRenderArgs.DefaultRenderArgs(info);

        // If there is no IGrabbable component we can return the default args
        if(iGrabbable == null)
            return args;

        // Try to get a render result from the iGrabbable
        GrabbableRenderArgs? renderResult = iGrabbable.Render(player.uid.Value.ToString());
        if(renderResult.HasValue) 
            args = renderResult.Value;

        string ApplyVariables(string text) 
        {
            if(iGrabbable == null)
                return text;

            Dictionary<string, string> variables = grabbable.GetIGrabbable().GetVariables();
            foreach(string key in variables.Keys) {
                text = text.Replace(key, variables[key]);
            }

            return text;
        }

        args.name = ApplyVariables(args.name);
        args.descriptionLines = args.descriptionLines.Select(line => $"<nobr>{ApplyVariables(line)}</nobr>").ToArray();

        return args;
    }

    private void UpdateTextElements(GrabbableRenderArgs renderArgs) 
    {
        nameText.text = renderArgs.name;
        descriptionText.text = string.Join("\n", renderArgs.descriptionLines);
    }

    private void UpdateSize(GrabbableRenderArgs renderArgs) 
    {
        RectTransform t = GetComponent<RectTransform>();
        Vector2 sizeDelta = t.sizeDelta;
        int descLineCnt = renderArgs.descriptionLines.Length;
        if(descLineCnt == 0)
            sizeDelta.y = heightNoDesc;
        else {
            sizeDelta.y = heightWithDesc + (descLineHeight * (descLineCnt-1));
        }
        t.sizeDelta = sizeDelta;            
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
    public static GrabbableRenderArgs CreateEmpty() { return new("", new string[0]); }
}