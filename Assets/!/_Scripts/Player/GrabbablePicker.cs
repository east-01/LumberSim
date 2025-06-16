using EMullen.Core;
using UnityEngine;

public class GrabbablePicker : MonoBehaviour 
{

    [SerializeField]
    private new Camera camera;
    [SerializeField]
    private float viewRange = 5f;

    private Grabbable lastSelectedGrabbable;

    private Player player;
    
    private void Awake()
    {
        player = GetComponentInParent<Player>();   
    }

    private void Update() {
        Grabbable newGrabbable = PickGrabbable(viewRange);

        if(newGrabbable != lastSelectedGrabbable) {
            BLog.Highlight($"Set grabbable to \"{newGrabbable}\"");
            ToggleOutlineView(lastSelectedGrabbable, false);
            ToggleOutlineView(newGrabbable, true);

            lastSelectedGrabbable = newGrabbable;

            player.GetHUD().GrabbableRenderer.Render(newGrabbable);
        }
    }

    private void ToggleOutlineView(Grabbable grabbable, bool selected) 
    {
        if(grabbable == null)
            return;

        grabbable.UpdateOutline(selected);
    }

    public Grabbable PickGrabbable(float range) => PickGrabbable(range, out RaycastHit hit);
    public Grabbable PickGrabbable(float range, out RaycastHit hit) 
    {
        Physics.Raycast(camera.transform.position, camera.transform.forward, out hit, range);
        if(hit.collider == null)
            return null;

        // We do this because we know that the TreeLogVisuals are a child of the TreeLog GameObject
        if(!FindOnObject(hit.collider.gameObject, out Grabbable result))
            return null;

        return result;
    }

    private bool FindOnObject<T>(GameObject obj, out T result) 
    {
        if(obj.TryGetComponent(out T onObj)) {
            result = onObj;
            return true;
        }
        if(obj.transform.parent != null && FindOnObject(obj.transform.parent.gameObject, out T onParent)) {
            result = onParent;
            return true;
        }
        result = default;
        return false;
    }
}