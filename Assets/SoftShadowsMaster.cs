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
    
    private uint currentSample = 0;
    private Material addMaterial;
    
    public Light DirectionalLight;
    
    //Range[(0.0,1.0)]
    //public float specularity_r = 1.0;
    
    //Range[(0.0,1.0)]
    //public float specularity_g = 0.78f;
    
    //Range[(0.0,1.0)]
    //public float specularity_b = 0.34f
    
    private void Awake()
    {
        camera = GetComponent<Camera>();
    }
    
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
    
        
        //Set shader parameters
        SoftShadowsShader.SetTexture(0,"SkyboxTexture", SkyboxTexture);
        SoftShadowsShader.SetMatrix("CameraToWorld", camera.cameraToWorldMatrix);
        SoftShadowsShader.SetMatrix("CameraInverseProjection", camera.projectionMatrix.inverse);
        SoftShadowsShader.SetVector("PixelOffset", new Vector2(Random.value, Random.value));
        Vector3 l = DirectionalLight.transform.forward;
        SoftShadowsShader.SetVector("DirectionalLight", new Vector4(l.x, l.y, l.z, DirectionalLight.intensity));
        //SoftShadowsShader.SetFloat("specularity_r", specularity_r);
        //SoftShadowsShader.SetFloat("specularity_g", specularity_g);
        //SoftShadowsShader.SetFloat("specularity_b", specularity_b);

        Render(destination);
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
            currentSample = 0;
        }
        
        // initialization finished
        
        
        SoftShadowsShader.SetTexture(0,"Result",target);
        int threadGroupsX = Mathf.CeilToInt(Screen.width/8.0f);
        int threadGroupsy = Mathf.CeilToInt(Screen.height/8.0f);
        SoftShadowsShader.Dispatch(0,threadGroupsX,threadGroupsy,1);
        
        // Blit the result texture to the screen
        if (addMaterial == null)
            addMaterial = new Material(Shader.Find("Hidden/ExtraShader"));
        addMaterial.SetFloat("_Sample", currentSample);
        Graphics.Blit(target, destination, addMaterial);
        currentSample++;
    }
    
    // Update is called once per frame
    void Update()
    {
    
        if (transform.hasChanged)
        {
            currentSample = 0;
            transform.hasChanged = false;
        }
        if(DirectionalLight.transform.hasChanged)
        {
            currentSample = 0;
            transform.hasChanged = false;
        
        }
        
    }
}
