# **[Compute Shaders](https://docs.unity3d.com/6000.3/Documentation/Manual/class-ComputeShader-introduction.html)**

This approach uses Compute Shaders running entirely on the GPU. By using a ComputeBuffer to store data on the GPU, we can perform massive parallel computations with zero CPU-to-GPU transfer overhead. Rendering is handled via a single DrawProcedural call.