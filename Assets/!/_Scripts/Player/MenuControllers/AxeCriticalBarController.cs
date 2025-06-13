using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.UI;

public class AxeCriticalBarController : MonoBehaviour 
{

    [Header("References")]
    [SerializeField]
    private ItemAssignments itemAssignments;
    [SerializeField]
    private RectTransform targetZoneTransform;
    [SerializeField]
    private RectTransform cursor;
    [SerializeField]
    private Image backgroundImage;
    [SerializeField]
    private MMF_Player criticalHitPlayer;
    public void PlayCriticalHit() => criticalHitPlayer.PlayFeedbacks();
    [SerializeField]
    private MMF_Player mediumHitPlayer;
    public void PlayMediumHit() => mediumHitPlayer.PlayFeedbacks();
    [SerializeField]
    private MMF_Player smallHitPlayer;
    public void PlaySmallHit() => smallHitPlayer.PlayFeedbacks();
    [SerializeField]
    private MMF_Player missPlayer;
    public void PlayMiss() => missPlayer.PlayFeedbacks();

    [Header("Settings")]
    [SerializeField]
    private float minAngle;
    [SerializeField]
    private float maxAngle;
    [SerializeField]
    private Color activeColor;
    [SerializeField]
    private float fadeSpeed = 10;

    /// <summary>
    /// The timestamp where the
    /// </summary>
    private float lastActive;

    public float targetValue;
    public float cursorValue;
    private Color targetBackgroundColor;

    private Player player;
    private ToolBelt toolBelt;
    private CanvasGroup canvasGroup;

    private Item lastCurrentItem;

    protected void Awake()
    {    
        player = GetComponentInParent<Player>();
        if(player == null) {
            Debug.LogError("Failed to get player in parent. It is assumed that the PlayerHUDMenuController is on a canvas that's a child of a Player GameObject.");
            return;
        }
        toolBelt = player.GetComponent<ToolBelt>();
        if(toolBelt == null) {
            Debug.LogError("Failed to get ToolBelt on Player component, it is assumed that the ToolBelt component is on the same GameObject as the Player.");
            return;
        }        
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        ToolBeltImpl toolBeltImpl = toolBelt.GetCurrentImplementation();
        bool isAxeImpl = toolBeltImpl is AxeImpl;

        EnableDisableCanvasGroup(isAxeImpl);

        if(!isAxeImpl)
            return;

        AxeImpl.AxePhase axePhase = (toolBeltImpl as AxeImpl).Phase;

        switch(axePhase) 
        {
            case AxeImpl.AxePhase.READY:
                cursorValue = 0;

                cursor.gameObject.SetActive(true);
                targetZoneTransform.gameObject.SetActive(false);
                targetBackgroundColor = activeColor;
                break;

            case AxeImpl.AxePhase.SWINGING:
                cursor.gameObject.SetActive(true);
                targetZoneTransform.gameObject.SetActive(true);
                targetBackgroundColor = activeColor;
                break;

            case AxeImpl.AxePhase.COOLDOWN:

                cursor.gameObject.SetActive(false);
                targetZoneTransform.gameObject.SetActive(false);
                targetBackgroundColor = Color.gray;
                break;
        }

        targetValue = Mathf.Clamp01(targetValue);
        cursorValue = Mathf.Clamp01(cursorValue);
        backgroundImage.color = Color.Lerp(backgroundImage.color, targetBackgroundColor, Time.deltaTime*fadeSpeed);

        cursor.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(minAngle, maxAngle, cursorValue));
        targetZoneTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(minAngle, maxAngle, targetValue));
    }

    public void Show(bool animate = false) 
    {
        if (animate) {
            canvasGroup.alpha = 1f; // ensure visible before anim
            var animator = GetComponent<Animator>();
            animator.Play("GrabbableRendererShow", 0, 0f);
            animator.SetTrigger("GrabbableRendererShow");
        } else {
            canvasGroup.alpha = 1f;
        }
    }

    public void Hide(bool animate = false) 
    {
        var animator = GetComponent<Animator>();
        if(animate) {
            animator.SetTrigger("GrabbableRendererHide");
        } else {
            canvasGroup.alpha = 0f;
        }
    }

    private void EnableDisableCanvasGroup(bool enabled) 
    {
        GetComponent<CanvasGroup>().alpha = enabled ? 0.7f : 0f;
        GetComponent<CanvasGroup>().blocksRaycasts = enabled;
        GetComponent<CanvasGroup>().interactable = enabled;
    }

}