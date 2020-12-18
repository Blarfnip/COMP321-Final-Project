# COMP 321: Programming Languages Final Project
## Exploration of HLSL/Cg through shaders in Unity
Developed by: Saul Amster, Bria Bradley, Emma Bieck  
  
This  project implements GPU accelerated ray tracing using Unity and HLSL compute shaders. The renderer is limited to spheres, the ground plane, and the skybox. The code also contains a C# translation of the base compute shader code for direct performance comparisons. 

### Installation
This project is using ```Unity LTS 2019.4.14f1```. It is an exploration of HLSL computer shaders, so a DirectX compatible GPU is also required. To open, run Unity and open the github root folder. Press the Play button to run. (GPU compute effects run without the game playing.)

### Checking Efficiency
Open the profiler through `Window > Analysis > Profiler` in Unity. When the engine is running (The play button was pressed), the profiler will record detailed frame timing. This can be analyzed to find individual timing of the code and functions on both the CPU and GPU (main thread and render thread).

### Resources
[Code based on tutorial from here](http://blog.three-eyed-games.com/2018/05/03/gpu-ray-tracing-in-unity-part-1/)  
  
Shader code found at ```RayTracing.compute``` contains extensive comments explaining the algorithm and math. Here are copies of the links referenced in the comments.  
[Skybox texture](https://hdrihaven.com/hdri/?c=skies&h=kiara_1_dawn)  
[Unity Docs on SamplerStates](https://docs.unity3d.com/Manual/SL-SamplerStates.html)  
[Comprehensive explanation of Unity Camera projection matrices](https://answers.unity.com/questions/1359718/what-do-the-values-in-the-matrix4x4-for-cameraproj.html)  
[Explanation of TRS matrix components](https://answers.unity.com/questions/402280/how-to-decompose-a-trs-matrix.html)  
[Unity Docs on TRS Matrices](https://docs.unity3d.com/ScriptReference/Matrix4x4.TRS.html)  
[Unity Docs on Camera Projection Matrix](https://docs.unity3d.com/ScriptReference/Camera-projectionMatrix.html)  
[Wiki Article on Equation for the Intersection of a Line with a Sphere](https://en.wikipedia.org/wiki/Line%E2%80%93sphere_intersection)  
[Microsoft Docs on reflect function](https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-reflect)  
[Wiki Article on conversion from Spherical coordinates to Cartesian for Skybox rendering](https://en.wikipedia.org/wiki/Spherical_coordinate_system#Coordinate_system_conversions)  
[Microsoft Docs on SampleLevel function](https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-to-samplelevel)  
[Microsoft Docs on Any function](https://docs.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-any)  
