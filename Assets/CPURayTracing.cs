using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CPURayTracing : MonoBehaviour
{
    public bool useAntiAliasing = true;
    public Texture2D skyboxTexture;
    public Light directionalLight;
    [Range(1, 8)]
    public int reflections = 3;
    private RenderTexture target;
    private Camera cam;

    private Material addMaterial;
    private uint currentSample;    

    private RayTracingCPU rayTracingCPU;

    void Awake() {
        cam = GetComponent<Camera>();
        rayTracingCPU = new RayTracingCPU();
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

        // rayTracingShader.SetTexture(0, "Result", target); // Give shader a reference to the output texture
        // rayTracingShader.SetTexture(0, "SkyboxTexture", skyboxTexture); // Give shader a reference to the skybox texture
        // rayTracingShader.SetMatrix("CameraToWorld", cam.cameraToWorldMatrix); // Give shader matrices for ray calculation
        // rayTracingShader.SetMatrix("CameraInverseProjection", cam.projectionMatrix.inverse);
        // rayTracingShader.SetVector("SubPixelOffset", new Vector2(Random.value, Random.value));
        // rayTracingShader.SetInt("ReflectionAmount", reflections);

        Vector3 l = directionalLight.transform.forward;
        // rayTracingShader.SetVector("DirectionalLight", new Vector4(l.x, l.y, l.z, directionalLight.intensity));
        
        // rayTracingShader.Dispatch(0, Mathf.CeilToInt(Screen.width / 8), Mathf.CeilToInt(Screen.height / 8), 1); // Start execution of shader

        rayTracingCPU.Result = target;
        rayTracingCPU.SkyboxTexture = skyboxTexture;
        rayTracingCPU.CameraToWorld = cam.cameraToWorldMatrix;
        rayTracingCPU.CameraInverseProjection = cam.projectionMatrix.inverse;
        rayTracingCPU.SubPixelOffset = new Vector2(Random.value, Random.value);
        rayTracingCPU.ReflectionAmount = (uint)reflections;
        rayTracingCPU.DirectionalLight = new Vector4(l.x, l.y, l.z, directionalLight.intensity);
        
        calculateShading(destination);

        // if(useAntiAliasing) {
        //     if(addMaterial == null)
        //         addMaterial = new Material(Shader.Find("Hidden/AddShader"));
        //     addMaterial.SetFloat("_Sample", currentSample);
        //     //Render Output
        //     Graphics.Blit(target, destination, addMaterial);
        // } else {
        //     Graphics.Blit(target, destination);
        // }


    }

    void calculateShading(RenderTexture destination) {
        Texture2D texture = new Texture2D(target.width, target.height);
        RenderTexture.active = target;
        texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);

        for(int x = 0; x < target.width; x++) {
            for(int y = 0; y < target.height; y++) {
                texture.SetPixel(x, y, rayTracingCPU.CSMain(new Vector2(x, y)));
            }
        }
        texture.Apply();
        RenderTexture.active = null;
        Graphics.Blit(texture, destination);
    }

    void Update() {
        if(transform.hasChanged) {
            currentSample = 0;
            transform.hasChanged = false;
        }
    }
}

class RayTracingCPU {
    // Each #kernel tells which function to compile; you can have many kernels

    // PI Const used for trig stuff
    const float PI = 3.14159265f;

    // User adjustable variable for determining amount of raycast bounces to calculate
    public uint ReflectionAmount = 3;

    // Create a RenderTexture with enableRandomWrite flag and set it
    // with cs.SetTexture
    public RenderTexture Result;

    // Used for sub-pixel moving of the rays.
    // This is used in conjunction with another shader for providing
    // temporal anti-aliasing
    public Vector2 SubPixelOffset;

    // Compressed values for the Directional light in the scene
    // Compressed: {position: (xyz), intensity: (w)}
    public Vector4 DirectionalLight;

    // Reference to Skybox HDRI texture
    // Example taken from here: https://hdrihaven.com/hdri/?c=skies&h=kiara_1_dawn
    public Texture2D SkyboxTexture;
    // Texture sampler for getting values from the texture.
    // This is an advanced way of sampling from textures and comes with the advantage
    // of minimizing samplers if more textures are needed.
    // Official Unity Docs: https://docs.unity3d.com/Manual/SL-SamplerStates.html
    // SamplerState samplerSkyboxTexture;

    // Matrices
    // Here is a really comprehensive explanation of the following matrices
    // https://answers.unity.com/questions/1359718/what-do-the-values-in-the-matrix4x4-for-cameraproj.html


    // TRS matrix used for converting from world space to Camera-local space
    // These matrices contain Translation, Rotation, and Scale data
    // Read about decomposing here: https://answers.unity.com/questions/402280/how-to-decompose-a-trs-matrix.html
    // Official Unity Docs: https://docs.unity3d.com/ScriptReference/Matrix4x4.TRS.html
    public Matrix4x4 CameraToWorld;

    // A Projection matrix is used for transforming points from Camera Space to Clip Space
    // We are using the inverse of that to convert a point in "clip space" (similar to view space)
    // to Camera space.
    // Official Unity Docs: https://docs.unity3d.com/ScriptReference/Camera-projectionMatrix.html
    public Matrix4x4 CameraInverseProjection;

    // Data object (struct) for defining a Ray.
    // Vector3 is a compound data type that acts as a 3d vector.
    // It has 3 float values: x, y, z (or r, g, b)
    // To create a ray three things are required, the position of the ray origin
    // the direction the ray is going from that origin, and the energy the ray has.
    // Energy is used for calculating reflections
    struct Ray {
        public Vector3 origin;
        public Vector3 direction;
        public Vector3 energy;
    };

    // Constructor for Ray structs
    Ray CreateRay(Vector3 origin, Vector3 direction) {
        Ray ray = new Ray();
        ray.origin = origin;
        ray.direction = direction;
        ray.energy = new Vector3(1.0f, 1.0f, 1.0f);
        return ray;
    }

    Ray CreateCameraRay(Vector2 uv)
    {
        // Grab the camera origin in world space from the CameraToWorld matrix.
        // The offset position of the camera can be found in the fourth column.
        // Multiply the matrix by the following Vector4 to get the fourth column
        // Position is stored in 3 values, so use .xyz to get a Vector3 and discard
        // the final value.
        Vector3 origin = (Vector3)(CameraToWorld * new Vector4(0.0f, 0.0f, 0.0f, 1.0f));

        // Use the Inverse Projection Matrix to convert from clip space (view space) to Camera Space.
        // uv is the scaled position on the screen from [-1, 1] where (0, 0) is the screen center
        Vector3 direction = (Vector3)(CameraInverseProjection * new Vector4(uv.x, uv.y, 0.0f, 1.0f));

        // Use the CameraToWorld matrix to convert the point from Camera Space to World Space
        direction = (Vector3)(CameraToWorld * new Vector4(direction.x, direction.y, direction.z, 0.0f));
        // Normalize the result direction vector. (Make its magnitude 1)
        direction = (direction).normalized;

        return CreateRay(origin, direction);
    }

    // Data object (struct) for defining the Ray collision
    // Ray Hits consist of:
    // The position of the collision in world space
    // The distance the ray traveled from the camera
    // The normal vector of the surface at the hit position
    struct RayHit {
        public Vector3 position;
        public float distance;
        public Vector3 normal;
    };

    // Constructor for RayHit structs
    // Values will be populated after constructed if hits occur
    // otherwise the ray retains these values (infinite distance ray)
    RayHit CreateRayHit() {
        RayHit hit;
        hit.position = new Vector3(0.0f, 0.0f, 0.0f);
        hit.distance = Mathf.Infinity; // This is shorthand for Infinity, although there is little docs for it
        hit.normal = new Vector3(0.0f, 0.0f, 0.0f);
        return hit;
    }

    // Function for tracing a fixed ground plane
    // HLSL parameters are pass by value by default. "inout" makes it pass by reference
    void IntersectGroundPlane(Ray ray, ref RayHit bestHit) {
        float floorHeight = 0;

        // Calculate the distance along the ray where the ground plane is intersected
        // I don't like this math, but it checks out if you write it down
        float t = -(ray.origin.y - floorHeight) / ray.direction.y;

        // if intersec distance is greater than 0 and less than Infinity or the other closest distance
        if(t > 0 && t < bestHit.distance) {

            // Populate hit values
            bestHit.distance = t;
            bestHit.position = ray.origin + (t * ray.direction);
            // Plane is flat along XZ axis, so the normal value will be in the Y axis
            bestHit.normal = new Vector3(0.0f, 1.0f, 0.0f);
        }
    }

    // Function for tracing a sphere defined as a Vector4 {position: (xyz), radius: (w)}
    // Math for intersection function can be found here: https://en.wikipedia.org/wiki/Line%E2%80%93sphere_intersection
    void IntersectSphere(Ray ray, ref RayHit bestHit, Vector4 sphere) {
        Vector3 d = ray.origin - ((Vector3)sphere);
        float p1 = -1f * Vector3.Dot(ray.direction, d);
        float p2sqr = p1 * p1 - Vector3.Dot(d, d) + sphere.w * sphere.w;
        if(p2sqr < 0)
            return;
        float p2 = Mathf.Sqrt(p2sqr);
        float t = p1 - p2 > 0 ? p1 - p2 : p1 + p2;
        if(t > 0 && t < bestHit.distance) {
            bestHit.distance = t;
            bestHit.position = ray.origin + (t * ray.direction);
            bestHit.normal = (bestHit.position - ((Vector3)sphere)).normalized;
        }
    }

    // Function used for calculating the intersection of a ray
    RayHit Trace(Ray ray) {
        RayHit bestHit = CreateRayHit();


        // Calculate intersections for the ground plane
        IntersectGroundPlane(ray, ref bestHit);

        // Calculate intersections fro 100 spheres
        for(int x = 0; x < 1; x++) {
            for(int z = 0; z < 1; z++) {
                for(int y = 0; y < 1; y++) {
                    IntersectSphere(ray, ref bestHit, new Vector4(x * 4.0f, 1.0f + (y * 4.0f), z * 4.0f, 1.0f));
                }
            }
        }
        return bestHit;
    }

    Vector3 Shade(ref Ray ray, RayHit hit) {
        if(hit.distance < Mathf.Infinity) {
            // Arbitrary specularity and albedo values
            // Modify these values for different results
            Vector3 specular = new Vector3(0.3f, 0.3f, 0.3f);
            Vector3 albedo = new Vector3(0.8f, 0.8f, 0.8f);

            // Set the origin to the ray hit point (but a little above the surface)
            ray.origin = hit.position + hit.normal * 0.001f;
            // Reflect the direction using the hit normal
            // Microsoft Docs: https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-reflect
            ray.direction = Vector3.Reflect(ray.direction, hit.normal);
            // Reduce ray energy by specular amount
            ray.energy = new Vector3(ray.energy.x * specular.x, ray.energy.y * specular.y, ray.energy.z * specular.z);

            // Calculate another ray for shadows.
            // Place the ray origin at the surface of the ray intersection
            Ray shadowRay = CreateRay(hit.position + hit.normal * 0.001f, -1 * ((Vector3)DirectionalLight));
            // Cast it towards the directional light        
            RayHit shadowHit = Trace(shadowRay);
            // If the ray hit something before reaching the skybox, then it is in shadow
            // return the shadow color
            if(shadowHit.distance != Mathf.Infinity) {
                return Vector3.zero;
            }

            // Use the dot product to calculate the amount of shadowing of an object.
            // Color is based on light intensity and albedo color
            return Mathf.Clamp(Vector3.Dot(hit.normal, ((Vector3)DirectionalLight) * -1), 0f, 1f) * DirectionalLight.w * albedo;
        } else {
            // Rays that hit the skybox have no reflections, so no energy
            ray.energy = Vector3.zero;

            // Skybox
            // If the ray never hits anything, use the direction to find the correct color
            // from the Skybox texture.
            // The direction is in Cartesian Coordinates, and must be converted to Spherical Coordinates
            // because that's how the Skybox texture is stored.
            // Wiki on Conversion Equations: https://en.wikipedia.org/wiki/Spherical_coordinate_system#Coordinate_system_conversions
            // (The x,y,z don't line up with the equations because Unity uses a different axis system)
            float theta = Mathf.Acos(ray.direction.y) / -PI;
            float phi = Mathf.Atan2(ray.direction.x, -ray.direction.z) / -PI * 0.5f;

            // SampleLevel is an advanced texture sampling function.
            // It samples a texture's LOD level/mip map level based on the last parameter. (0 being the most detailed)
            // The Vector2 represents the coordinates on the texture to sample.
            // Microsoft DirectX Docs: https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-to-samplelevel
            Color skyColor = SkyboxTexture.GetPixel(Mathf.RoundToInt(phi * SkyboxTexture.width), Mathf.RoundToInt(theta * SkyboxTexture.height));
            return new Vector3(skyColor.r, skyColor.g, skyColor.b);
        }
    }

    public Color CSMain (Vector2 id)
    {
        // Get the dimensions of the RenderTexture (The dimensions of the screen)
        uint width, height;
        width = (uint)Result.width;
        height = (uint)Result.height;

        // Convert pixel location to normalized position [0.0, 1.0]
        Vector2 uv = (id + SubPixelOffset) / new Vector2(width, height);
        // Convert to Clip Space [-1.0, 1.0]
        uv = (uv * 2.0f);
        uv.x -= 1.0f;
        uv.y -= 1.0f;

        // Create a ray using the uv calculated using this pixel coordinate
        Ray ray = CreateCameraRay(uv);

        Vector3 result = new Vector3(0,0,0);
        for(uint i = 0; i < ReflectionAmount; i++) {
            // Raytrace the ray
            RayHit hit = Trace(ray);

            Vector3 preShadeEnergy = ray.energy;
            // Shade the resulting hit
            Vector3 shadedRay = Shade(ref ray, hit);
            shadedRay = new Vector3(shadedRay.x * preShadeEnergy.x, shadedRay.y * preShadeEnergy.y, shadedRay.z * preShadeEnergy.z);
            result += shadedRay;

            // if the ray has no energy left, bail
            // any function Docs: https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-any
            if((ray.energy).sqrMagnitude <= 0.0000001f)
                break;
        }
        

        // Output the final shaded value for the pixel
        return new Color(result.x, result.y, result.z, 1f);
    }

}
