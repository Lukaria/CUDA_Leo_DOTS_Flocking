
# **[Unity DOTS](https://unity.com/dots)**

This ECS approach utilizes the Unity DOTS stack (Jobs, Burst Compiler, Entities, and Entities Graphics), allowing for highly optimized multi-threaded code. It offers a suite of tools designed to simplify ECS-style development while achieving high performance.

All particles are instantiated from a Prefab and rendered via [Entities.Graphics](https://docs.unity3d.com/Packages/com.unity.entities.graphics@1.0/manual/index.html). 

**Implementation Details:**
- **Components:** Since the standard `LocalTransform` component already exists on entities, additional Position and Rotation components are not used.
- **Optimization:** For performance reasons, the Spatial Hash flat arrays are implemented directly inside the Simulation System rather than in a separate class.