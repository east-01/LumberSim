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

    private CanvasGroup canvasGroup;

    private void Awake()
    {
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

        nameText.text = ApplyVariables(info.DisplayName);
        descriptionText.text = string.Join("\n", info.DescriptionLines.Select(line => ApplyVariables(line)));
    }
}