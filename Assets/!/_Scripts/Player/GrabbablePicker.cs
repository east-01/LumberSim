using EMullen.Core;
using UnityEngine;

public class GrabbablePicker : MonoBehaviour 
{

    [SerializeField]
    private new Camera camera;
    [SerializeField]
    private float viewRange = 5f;

    public Grabbable SelectedGrabbable { get; private set; }
    /// <summary>
    /// Is the SelectedGrabbable manually selected, manual selection comes from HandsImpl picking
    ///   up a grabbable.
    /// </summary>
    private bool manuallySelected;

    private Player player;
    
    private void Awake()
    {
        player = GetComponentInParent<Player>();   
    }

    private void Update() {
        Grabbable newGrabbable = PickGrabbable(viewRange);

        if(SelectedGrabbable != null && !SelectedGrabbable.IsSpawned)
            ClearSelectedGrabbable();

        if(newGrabbable != SelectedGrabbable && !manuallySelected) {
            SetSelectedGrabbable(newGrabbable);
        }
    }

    public void SetSelectedGrabbable(Grabbable grabbable, bool manuallySelected = false) 
    {
        if(SelectedGrabbable != null)
            SelectedGrabbable.UpdateOutline(false);

        if(grabbable != null)
            grabbable.UpdateOutline(true);

        SelectedGrabbable = grabbable;
        this.manuallySelected = manuallySelected;
    }

    public void ClearSelectedGrabbable() 
    {
        if(SelectedGrabbable != null)
            SelectedGrabbable.UpdateOutline(false);

        SelectedGrabbable = null;
        manuallySelected = false;
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