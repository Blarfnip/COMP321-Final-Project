using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class SoftShadowsMaster : MonoBehaviour
{

    public ComputeShader SoftShadowsShader;
    
    private RenderTexture target;
    
    public Texture2D SkyboxTexture;
    
    private Camera camera;
    
    private void Awake()
    {
        camera = GetComponent<Camera>();
    }
    
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        Render(destination);
        //Set shader parameters
        SoftShadowsShader.SetTexture(0,"SkyboxTexture", SkyboxTexture);
        SoftShadowsShader.SetMatrix("CameraToWorld", camera.cameraToWorldMatrix);
        SoftShadowsShader.SetMatrix("CameraInverseProjection", camera.projectionMatrix.inverse);
        
    }
    
    private void Render(RenderTexture destination)
    {
        //initializes render texture
        if(target == null || target.width != Screen.width || target.height!=Screen.height)
        {
            if(target != null)
                target.Release();
            
            target = new RenderTexture(Screen.width, Screen.height, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            target.enableRandomWrite = true;
            target.Create();
        }
        
        // target texture initialized
        
        
        SoftShadowsShader.SetTexture(0,"Result",target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width/8.0f);
        int threadGroupsy = Mathf.CeilToInt(Screen.height/8.0f);
        SoftShadowsShader.Dispatch(0,threadGroupsX,threadGroupsy,1);
        
        Graphics.Blit(target, destination);
    }
    
    // Update is called once per frame
    void Update()
    {
        
    }
}
