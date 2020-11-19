using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastDispatcher : MonoBehaviour
{
    public ComputeShader rayTracingShader;
    private RenderTexture target;

    void OnRenderImage(RenderTexture source, RenderTexture destination) {
        
        //Init render texture
        if(target == null || target.width != Screen.width || target.height != Screen.height) {

            if(target != null) {
                target.Release();
            }

            target = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            target.enableRandomWrite = true;
            target.Create();
        }

        //Setup and dispatch compute shader

        rayTracingShader.SetTexture(0, "Result", target);
        rayTracingShader.Dispatch(0, Mathf.CeilToInt(Screen.width / 8), Mathf.CeilToInt(Screen.height / 8), 1);

        //Render Output
        Graphics.Blit(target, destination);
    }
}
