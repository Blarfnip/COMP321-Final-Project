using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class RaycastDispatcher : MonoBehaviour
{
    public ComputeShader rayTracingShader;
    public Texture2D skyboxTexture;
    private RenderTexture target;
    private Camera cam;

    void Awake() {
        cam = GetComponent<Camera>();
    }
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

        rayTracingShader.SetTexture(0, "Result", target); // Give shader a reference to the output texture
        rayTracingShader.SetTexture(0, "SkyboxTexture", skyboxTexture); // Give shader a reference to the skybox texture
        rayTracingShader.SetMatrix("CameraToWorld", cam.cameraToWorldMatrix); // Give shader matrices for ray calculation
        rayTracingShader.SetMatrix("CameraInverseProjection", cam.projectionMatrix.inverse);
        
        rayTracingShader.Dispatch(0, Mathf.CeilToInt(Screen.width / 8), Mathf.CeilToInt(Screen.height / 8), 1); // Start execution of shader

        //Render Output
        Graphics.Blit(target, destination);
    }
}
