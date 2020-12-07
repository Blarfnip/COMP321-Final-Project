using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class RaycastDispatcher : MonoBehaviour
{
    public bool useAntiAliasing = true;
    public ComputeShader rayTracingShader;
    public Texture2D skyboxTexture;
    public Light directionalLight;
    [Range(1, 8)]
    public int reflections = 3;
    private RenderTexture target;
    private Camera cam;

    private Material addMaterial;
    private uint currentSample;

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
        rayTracingShader.SetVector("SubPixelOffset", new Vector2(Random.value, Random.value));
        rayTracingShader.SetInt("ReflectionAmount", reflections);

        Vector3 l = directionalLight.transform.forward;
        rayTracingShader.SetVector("DirectionalLight", new Vector4(l.x, l.y, l.z, directionalLight.intensity));
        
        rayTracingShader.Dispatch(0, Mathf.CeilToInt(Screen.width / 8), Mathf.CeilToInt(Screen.height / 8), 1); // Start execution of shader

        if(useAntiAliasing) {
            if(addMaterial == null)
                addMaterial = new Material(Shader.Find("Hidden/AddShader"));
            addMaterial.SetFloat("_Sample", currentSample);
            //Render Output
            Graphics.Blit(target, destination, addMaterial);
        } else {
            Graphics.Blit(target, destination);
        }


    }

    void Update() {
        if(transform.hasChanged) {
            currentSample = 0;
            transform.hasChanged = false;
        }
    }
}
