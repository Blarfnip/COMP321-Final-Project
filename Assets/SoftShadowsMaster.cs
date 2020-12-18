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
    
    public bool antiAliasing =true;
    
    
    [Header("_Spheres")]
    public Vector2 SphereRadius = new Vector2(3.0f, 8.0f);
    public uint SpheresMax = 50;
    public float SpherePlacementRadius = 100.0f;
    
    private ComputeBuffer _sphereBuffer;
    
    struct Sphere{
        public Vector3 position;
        public float radius;
        public Vector3 albedo;
        public Vector3 specular;
    }
    
    private void OnEnable()
    {
        currentSample = 0;
        SetUpScene();
    }
    private void OnDisable()
    {
        if (_sphereBuffer != null)
            _sphereBuffer.Release();
    }
    private void SetUpScene()
    {
        List<Sphere> spheres = new List<Sphere>();
        // Add a number of random spheres
        for (int i = 0; i < SpheresMax; i++)
        {
            Sphere sphere = new Sphere();
            // Radius and radius
            sphere.radius = SphereRadius.x + Random.value * (SphereRadius.y - SphereRadius.x);
            Vector2 randomPos = Random.insideUnitCircle * SpherePlacementRadius;
            sphere.position = new Vector3(randomPos.x, sphere.radius, randomPos.y);
            // Reject spheres that are intersecting others
            foreach (Sphere other in spheres)
            {
                float minDist = sphere.radius + other.radius;
                if (Vector3.SqrMagnitude(sphere.position - other.position) < minDist * minDist)
                    goto SkipSphere;
            }
            // Albedo and specular color
            Color color = Random.ColorHSV();
            //Color colorSpecular = new Vector3();
            //bool metal = Random.value < 0.5f;
            bool metal = true;
            sphere.albedo = metal ? new Vector3(0.2f,0.2f,0.2f) : new Vector3(color.r, color.g, color.b);
            sphere.specular = metal ? new Vector3(color.r, color.g, color.b) : Vector3.one * 0.04f;
            // Add the sphere to the list
            spheres.Add(sphere);
        SkipSphere:
            continue;
        }
        // Assign to compute buffer
        if (_sphereBuffer != null)
            _sphereBuffer.Release();
        if (spheres.Count > 0)
        {
            _sphereBuffer = new ComputeBuffer(spheres.Count, 40);
            _sphereBuffer.SetData(spheres);
        }
    }

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
        SoftShadowsShader.SetBuffer(0, "_Spheres", _sphereBuffer);

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
        if(antiAliasing){
            if (addMaterial == null)
                addMaterial = new Material(Shader.Find("Hidden/ExtraShader"));
            addMaterial.SetFloat("_Sample", currentSample);
            Graphics.Blit(target, destination, addMaterial);
            currentSample++;
        }
        else{Graphics.Blit(target, destination);}
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
