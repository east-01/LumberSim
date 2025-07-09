using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class PassRotationToShader : MonoBehaviour
{
    private MaterialPropertyBlock block;
    private Graphic graphic;
    private RectTransform rectTransform;

    private void Awake()
    {
        graphic = GetComponent<Graphic>();
        rectTransform = GetComponent<RectTransform>();
        block = new MaterialPropertyBlock();
    }

    void Update()
    {
        float angle = rectTransform.eulerAngles.z * Mathf.Deg2Rad;

        Vector2 cosSin = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        graphic.canvasRenderer.GetMaterial().SetVector("_RotationData", cosSin);
    }
}
