using EMullen.Core;
using UnityEngine;

public class RaycastPicker : MonoBehaviour 
{

    [Header("References")]
    [SerializeField]
    private new Camera camera;
    [SerializeField]
    private Player player;

    [Header("Settings")]
    [SerializeField]
    private float viewRange = 5f;

    // Variables
    public Selectable CurrentSelectable { get; private set; }
    
    private void Update() {
        Selectable newGrabbable = PickSelectable(viewRange);
        if(newGrabbable != CurrentSelectable) {
            SetSelectedGrabbable(newGrabbable);
        }
    }

    private void SetSelectedGrabbable(Selectable selectable) 
    {
        if(CurrentSelectable != null)
            CurrentSelectable.UpdateSelectable(false);

        if(selectable != null)
            selectable.UpdateSelectable(true);

        CurrentSelectable = selectable;
    }

    public void ClearSelectedGrabbable() 
    {
        if(CurrentSelectable != null)
            CurrentSelectable.UpdateSelectable(false);

        CurrentSelectable = null;
    }

    public Selectable PickSelectable(float range) => PickSelectable(range, out RaycastHit hit);
    public Selectable PickSelectable(float range, out RaycastHit hit) 
    {
        Physics.Raycast(camera.transform.position, camera.transform.forward, out hit, range);
        if(hit.collider == null)
            return null;

        // We do this because we know that the TreeLogVisuals are a child of the TreeLog GameObject
        if(!FindOnObject(hit.collider.gameObject, out Selectable result))
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