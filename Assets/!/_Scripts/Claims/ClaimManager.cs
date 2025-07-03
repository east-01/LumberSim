

using UnityEngine;

[ExecuteInEditMode]
public class ClaimManager : MonoBehaviour 
{
    [SerializeField]
    private LineRenderer lineRenderer;
    [SerializeField]
    private float targetLineSize;


    private void Update()
    {
        lineRenderer.SetPosition(0, new(-targetLineSize, 0, -targetLineSize));
        lineRenderer.SetPosition(1, new(-targetLineSize, 0, targetLineSize));
        lineRenderer.SetPosition(2, new(targetLineSize, 0, targetLineSize));
        lineRenderer.SetPosition(3, new(targetLineSize, 0, -targetLineSize));
    }
}