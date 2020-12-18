using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class RayTracingObject : MonoBehaviour
{
    private void OnEnable()
    {
        RayTracingMain.RegisterObject(this);
    }

    private void OnDisable()
    {
        RayTracingMain.UnregisterObject(this);
    }
}