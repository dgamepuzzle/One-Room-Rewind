using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ImageEffectShader : MonoBehaviour {
    [SerializeField]
    Material material;

    public bool ExecuteInEditMode = false;

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!ExecuteInEditMode && !Application.isPlaying)
        {
            Graphics.Blit(source, destination);
            return;
        }
        Graphics.Blit(source, destination, material);
    }
}
