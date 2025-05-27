using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class Grabbable : NetworkBehaviour
{
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

    private void FixedUpdate()
    {
        var rawVelocity = (transform.position - lastPos) / Time.fixedDeltaTime;
        PercievedVelocity = rawVelocity;

        SmoothedPerceivedVelocity = Vector3.Lerp(SmoothedPerceivedVelocity, rawVelocity, velocitySmoothing);
        if(SmoothedPerceivedVelocity.magnitude > maxPercievedVelocity)
            SmoothedPerceivedVelocity = SmoothedPerceivedVelocity.normalized * maxPercievedVelocity;

        lastPos = transform.position;
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
    public Dictionary<string, string> GetVariables();
    public GrabbableInfo OverrideGrabbableInfo() => null;
}