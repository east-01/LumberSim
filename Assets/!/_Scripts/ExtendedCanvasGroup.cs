using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ExtendedCanvasGroup : MonoBehaviour
{
    public CanvasGroup canvasGroup;

    [Header("GameObject set active/disabled if alpha !=/== 0")]
    public List<GameObject> syncedObjects;

    private float observedAlpha;

    private void Start()
    {
        ForceUpdate();
    }

    private void Update()
    {
        if(canvasGroup.alpha == observedAlpha)
            return;

        ForceUpdate();
    }

    public void ForceUpdate() 
    {
        foreach(GameObject go in syncedObjects) {
            go.SetActive(canvasGroup.alpha > 0);
        }

        observedAlpha = canvasGroup.alpha;
    }
}