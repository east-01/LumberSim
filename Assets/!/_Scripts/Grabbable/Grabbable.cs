using System.Collections;
using System.Collections.Generic;
using EMullen.Core;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

public class Grabbable : NetworkBehaviour
{
    [SerializeField]
    private Color defaultSelectColor;
    [SerializeField]
    private Color defaultNormalColor;

    private Outline _selectOutline;
    public Outline SelectOutline {
        get {
            // If we already have the select outline stored we don't need to find a new one.            
            if(_selectOutline != null)
                return _selectOutline;

            // Find a new Outline
            GameObject outlineObject = gameObject; // Set as self gameObject by default
            IGrabbable grabbable = GetIGrabbable();
            if(grabbable != null && grabbable.GetOutlineObject() != null)
                outlineObject = grabbable.GetOutlineObject();

            if(outlineObject.TryGetComponent(out Outline selectOutlineExisting)) {
                _selectOutline = selectOutlineExisting;
                return selectOutlineExisting;
            }
            
            _selectOutline = outlineObject.AddComponent<Outline>();
            return _selectOutline;
        }
        private set => _selectOutline = value;
    }

    [SerializeField]
    private GrabbableInfo info;
    public GrabbableInfo Info { get {
        IGrabbable grabbable = GetIGrabbable();
        if(grabbable != null && grabbable.OverrideGrabbableInfo() != null)
            return grabbable.OverrideGrabbableInfo();
        
        return info;
    } }
    [SerializeField] 
    private float velocitySmoothing = 0.1f;
    [SerializeField]
    private float maxPercievedVelocity = 10f;


    private Vector3 lastPos;
    public Vector3 PercievedVelocity { get; private set; }
    public Vector3 SmoothedPerceivedVelocity { get; private set; }

    private void Start()
    {
        StartCoroutine(UpdateOutlineInASecondCoroutine());
    }

    public IEnumerator UpdateOutlineInASecondCoroutine() 
    {
        yield return new WaitForSeconds(1f);
        UpdateOutline();      
    }

    private void FixedUpdate()
    {
        var rawVelocity = (transform.position - lastPos) / Time.fixedDeltaTime;
        PercievedVelocity = rawVelocity;

        SmoothedPerceivedVelocity = Vector3.Lerp(SmoothedPerceivedVelocity, rawVelocity, velocitySmoothing);
        if(SmoothedPerceivedVelocity.magnitude > maxPercievedVelocity)
            SmoothedPerceivedVelocity = SmoothedPerceivedVelocity.normalized * maxPercievedVelocity;

        lastPos = transform.position;
    }

    public void UpdateOutline(bool selected = false) 
    {
        // grabbable.SelectOutline.enabled = selected;
        Color selectColor = defaultSelectColor;
        Color normalColor = defaultNormalColor;
        if(Info.OutlineColors.Length == 1) {
            selectColor = Info.OutlineColors[0];
            normalColor = Info.OutlineColors[0];
        } else if(Info.OutlineColors.Length >= 2) {
            selectColor = Info.OutlineColors[0];
            normalColor = Info.OutlineColors[1];
        }

        SelectOutline.OutlineColor = selected ? selectColor : normalColor;
        SelectOutline.OutlineWidth = selected ? 8 : 4;
        SelectOutline.OutlineMode = Outline.Mode.OutlineVisible;
    }

    public IGrabbable GetIGrabbable() 
    {
        if(gameObject.TryGetComponent(out IGrabbable onObjGrab))
            return onObjGrab;
        if(gameObject.transform.parent != null && gameObject.transform.parent.gameObject.TryGetComponent(out IGrabbable onParentGrab))
            return onParentGrab;

        Debug.LogWarning($"Can't get Grabbable on game object \"{gameObject.name}\"");
        return null;
    }

}

public interface IGrabbable 
{
    public GrabbableRenderArgs? Render(string viewingPlayer = null) => null;
    public Dictionary<string, string> GetVariables();
    public bool CanPickup(NetworkConnection pickupConnection, string pickupUID, out string reason);
    public GrabbableInfo OverrideGrabbableInfo() => null;
    public GameObject GetOutlineObject() => null;
}