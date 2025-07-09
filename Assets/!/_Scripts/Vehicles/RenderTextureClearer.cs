
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class RenderTextureClearer : MonoBehaviour 
{
    private RawImage rawImg;

    private void Awake() 
    {
        rawImg = GetComponent<RawImage>();
    }

    private void LateUpdate() 
    {
        RenderTexture.active = rawImg.mainTexture as RenderTexture;
        GL.Clear(true, true, Color.clear);
        RenderTexture.active = null;
    }
}