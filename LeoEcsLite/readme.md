\# \*\*LeoEcsLite Approach\*\*

This ECS approach utilizes the LeoEcsLite framework by Leopotam, along with a slightly modified \[EcsLite Threads](https://github.com/Leopotam/ecslite-threads)  extension.



To compile this project, you only need to install the core LeoEcsLite framework. You can find installation instructions on the \[official GitHub page](https://github.com/Leopotam/ecslite). Note that you do not need to add EcsLite Threads separately, as a modified version is already included in the Assets folder for development convenience.



The simulation uses BoidsSystemThread to parallelize computations and \[Graphics.DrawMeshInstancedProcedural](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Graphics.DrawMeshInstancedProcedural.html) for efficient rendering.



Known Issue: During experiments and benchmarking, I noticed that performance degrades rapidly when increasing the simulation Bounds (used to limit particle positions). Although the algorithm matches the other approaches and the boundary math is trivial, I have not yet identified the root cause. If you find the solution, please let me know via GitHub issue!

