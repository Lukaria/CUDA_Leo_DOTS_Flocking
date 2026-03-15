# Murmuration Simulation

A [murmuration](https://uncertainty.club/murmuration/) (flocking) simulation of particles implemented using three different approaches: **LeoEcsLite with Threads**, **Unity DOTS stack**, and **Compute Shaders**.

Below are GIFs showing real-life murmuration alongside the final result from one of the implemented approaches.

<p align="center">
    <img src="https://github.com/Lukaria/CUDA_Leo_DOTS_Flocking/blob/main/media/murmuration.gif" width="50%" />
    <img src="https://github.com/Lukaria/CUDA_Leo_DOTS_Flocking/blob/main/media/simulation.gif" width="70%" />
</p>

## Table of Contents
- [Project Overview](#project-overview)
- [Theory & Algorithms](#theory--algorithms)
    - [The Flocking Algorithm](#the-flocking-algorithm)
    - [Spatial Hashing](#spatial-hashing)
    - [ECS](#ecs)
- [Approaches](#approaches)
- [Benchmarks](#benchmarks)
- [Suggestions for Improvement](#suggestions-for-improvement)
- [What's Next](#whats-next)

## Project Overview
This repository contains three different approaches to creating a flocking simulation, split into separate folders with their own documentation. Each approach utilizes the same core algorithm described in the [Theory](#theory--algorithms) section. You can also find performance benchmarks for all approaches in the [Benchmarks](#benchmarks) section.

## Theory & Algorithms

### The Flocking Algorithm
The **Boids** algorithm was chosen for this simulation. It is a well-known algorithm developed by Craig Reynolds in 1986. The algorithm calculates three vectors and applies their sum to the velocity of a particle (or "boid").

For each particle, the algorithm calculates a steering vector based on the particle's neighbors within a predefined radius. The three vectors are:
- **Cohesion:** Steer to move towards the average position (center of mass) of neighbors.
<p align="center">
    <img src="https://github.com/Lukaria/CUDA_Leo_DOTS_Flocking/blob/main/media/cohesion.png" width="25%" />
</p>

- **Separation:** Steer to avoid crowding local neighbors.
<p align="center">
    <img src="https://github.com/Lukaria/CUDA_Leo_DOTS_Flocking/blob/main/media/separation.png" width="25%" />
</p>

- **Alignment:** Steer towards the average heading of local neighbors.
<p align="center">
    <img src="https://github.com/Lukaria/CUDA_Leo_DOTS_Flocking/blob/main/media/alignment.png" width="25%" />
</p>

The calculations can be represented by the pseudocode below. We additionally use weights to enhance or diminish the final vector.

**Note:** In the actual code, we use **squared magnitude** instead of length for distance checks. This is much faster to compute (avoids square roots) and does not affect the final logic.

#### Cohesion
Cohesion is calculated as the distance between the particle's position and the average position of all neighbors.

```csharp
Vector3 cohesion = Vector3.zero;
for(int neighborId = 0; neighborId < neighborsList; neighborId++){
    Vector3 distance = length(neighborsList[neighborId].Position - particle.Position)
    if(length(distance) < cohesionRadius){
        cohesion += neighborsList[neighborId].Position;
    }
}

if (cohesionCount > 0){
    cohesion /= cohesionCount;
    cohesion = (cohesion - particle.Position) * cohesionWeight;
}
```
#### Separation
Separation is calculated as the average inverse distance vector between a boid and its neighbors. Since we want to consider the distance (the closer the neighbor, the stronger the force) and reduce calculation complexity, we divide by the squared distance.

```csharp
Vector3 separation = Vector3.zero;
for(int neighborId = 0; neighborId < neighborsList; neighborId++){
    Vector3 distance = length(neighborsList[neighborId].Position - particle.Position)
    if(length(distance) < separationRadius){
        separation += distance / lengthSq(distance);
        ++separationCount;
    }
}

if (separationCount > 0){
    separation /= separationCount;
    separation *= separationWeight;
}
```
#### Alignment
Alignment is calculated as the average velocity of all neighbors.

```csharp
Vector3 alignment = Vector3.zero;
for(int neighborId = 0; neighborId < neighborsList; neighborId++){
    Vector3 distance = length(neighborsList[neighborId].Position - particle.Position)
    if(length(distance) < alignmentRadius){
        alignment += neighborsList[neighborId].Velocity;
        ++alignmentCount;
    }
}

if (alignmentCount > 0){
    alignment /= alignmentCount;
    alignment *= alignmentWeight;
}
```

#### Spatial Hashing
How do we effectively find all neighbors? The first thing that comes to (my, not clever to be honest) mind is using a Hash Map to store boid IDs. We could hash the particle position and store its ID in a bucket.

However, there are two problems: we have to traverse all boids to compare distances, and standard Hash Maps can cause many cache misses. Instead, we can divide our 3D space into cubes (cells) with an edge length equal to the largest force radius. With this approach, we only need to traverse the cubes closest to our particle.

```csharp

Vector3 coordinate = position / maxRadius;
for (var dx = -1; dx <= 1; dx++)
    for (var dy = -1; dy <= 1; dy++)
        for (var dz = -1; dz <= 1; dz++) {
            var hash = Hash(coordinate.x + dx, coordinate.y + dy, coordinate.z + dz);
            // computations
        }
```
We can also use two flat arrays (called `bucket` and `next`) instead of a Dictionary/Hash Map. When we add a new particle, we compute the position hash, store `bucket[hash]` in `next[id]`, and only then store the particle ID in the bucket. This represents a classic Spatial Hash algorithm implemented with flat arrays (linked list style).
```
public void Insert(int particleId, Vector3 position)
{
    var key = Hash(position);

    _next[entityId] = _bucket[key];
    _bucket[key] = entityId;
}
```

There is a [great scientific article](https://dl.acm.org/doi/epdf/10.1145/1658866.1658870) (where I also got these beautiful images from) about boids and small description of such spatial hash approach

### ECS
Finally, the Entity Component System (ECS) is an architectural pattern used to manage and process game data. Unlike OOP, where a programmer creates methods to process data inside the same class where the data is stored, ECS uses Components that act as pure data containers (similar to DTOs).

A bunch of different components are stored in an Entity. Entities are stored in a World. A System is essentially a loop that iterates over Entities with a specific set of Components to process their data.

For example, to move all enemies, you can filter entities that have a SteeringComponent, HealthComponent, and EnemyComponent.

<p align="center">
    <img src="https://github.com/Lukaria/CUDA_Leo_DOTS_Flocking/blob/main/media/ecs.png" width="40%" />
</p>

You also can read this pretty [good article about ECS](https://arielcoppes.dev/2023/07/13/design-decisions-when-building-games-using-ecs.html) and [this FAQ](https://github.com/SanderMertens/ecs-faq?tab=readme-ov-file#entity-component-system-faq) for better understanding.

## Approaches
1. **[LeoEcsLite](https://github.com/Lukaria/CUDA_Leo_DOTS_Flocking/tree/main/LeoEcsLite)**

The first approach uses the LeoEcsLite framework created by Leopotam, with a slightly modified [EcsLite Threads](https://github.com/Leopotam/ecslite-threads) extension. I really enjoy this framework; it is lightweight, engine-agnostic, and uses structs for Components, which provides better cache locality.

2. **[Unity DOTS](https://github.com/Lukaria/CUDA_Leo_DOTS_Flocking/tree/main/DOTS)**

The second approach uses the Unity DOTS stack (Jobs, Burst Compiler, Entities, and Entities Graphics), which allows for highly optimized multi-threaded code. It offers many useful tools aimed at simplifying ECS-style development and achieving high performance.

3. **[Compute Shaders](https://github.com/Lukaria/CUDA_Leo_DOTS_Flocking/tree/main/ComputeShaders)**

The third approach uses Compute Shaders running entirely on the GPU. By using a ComputeBuffer to store data on the GPU, we can perform massive parallel computations with zero CPU-to-GPU transfer overhead. Rendering is handled via a single DrawProcedural call.

**Note:** Naturally, none of these approaches use standard Unity Physics, ensuring maximum performance.

## Benchmarks
Regarding the benchmarks, I should first point out that performance testing wasn't the main goal of this project, so the results might be somewhat rough and not perfectly accurate. Additionally, I ran them directly in the Unity Editor, meaning engine overhead definitely affected the results. However, there are still some interesting takeaways.

My setup is: RTX 5070 Ti + Intel Core i5-14400f (2500 Mhz, 10 Cores,16 Logical Processors) + RAM 2x16 DDR (3600 Mhz) (haven't you seen DDR5 prices?💀)

All benchmarks were captured using the exact same parameters over a fixed time period. The parameters are listed below:

- Entities: 5000
- Bounds Size: 20x20x20
- Cohesion Radius: 3
- Cohesion Weight: 2
- Separation Radius: 2
- Separation Weight: 7
- Alignment Radius: 1
- Alignment Weight: 1
- Min Speed: 1
- Max Speed: 1
- Rotation Speed: 4

Benchmarks:

| Parameter                    | LeoEcsLite | DOTS    | DOTS + Burst | ComputeShader |
|------------------------------|------------|---------|--------------|---------------|
| Frametime (ms)               |            |         |              |               |
| Average                      | 36.09      | 349.45  | 333.32       | 6.80          |
| Median                       | 25.26      | 224.94  | 241.62       | 5.60          |
| Minimum                      | 20.11      | 140.06  | 163.34       | 5.14          |
| Maximum                      | 4281.09    | 4285.01 | 3797.93      | 3868.34       |
| Worst 1%                     | 656.32     | 4285.01 | 3797.93      | 116.24        |
| Worst 1% (FPS)               | 1.5        | 0.2     | 0.3          | 8.6           |
| Worst 0.1%                   | 4281.09    | 3797.93 | 333.32       | 1249.72       |
| Worst 0.1% (FPS)             | 0.2        | 0.2     | 0.3          | 0.8           |
| Standard deviation           | 145.77     | 445.80  | 384.73       | 57.46         |
| CPU Usage (%)                |            |         |              |               |
| CPU Cores used               | 43.4       | 13.63   | 13.59        | 1.98          |
| Memory (Avg Mb)              |            |         |              |               |
| Total Allocated              | 4407       | 4587    | 4615         | 4095          |
| Total Reserved               | 5025       | 5116    | 2142         | 4635          |
| Managed Used                 | 798        | 770     | 777          | 778           |
| Managed Heap                 | 1272       | 1272    | 1272         | 1264          |
| GFX Driver                   | 4610       | 3177    | 3365         | 2310          |
| GPU frametime (ms)           |            |         |              |               |
| Average                      | 6.53       | 8.58    | 6.97         | 4.10          |
| Median                       | 6.37       | 9.13    | 7.62         | 4             |
| Maximum                      | 36.62      | 19.89   | 19.18        | 16.17         |
| Worst 1%                     | 10.78      | 15.53   | 13.76        | 6.39          |
| GPU Driver Memory (Mb)       | 4313       | 2880    | 3068         | 2012          |
| Throughput (particles / avg) | 766        | 583     | 717          | 1220          |


As expected, the Compute Shader approach significantly outperformed all others. I should also note that LeoEcsLite generally performed better than DOTS (both with and without Burst), which is quite impressive. 
However, I discovered that LeoEcsLite's performance degrades rapidly as the world bounds increase. For instance, with a bounding volume of 20x20x20, here is how it compared to DOTS:

| Parameter                    | LeoEcsLite | DOTS    | DOTS + Burst |
|------------------------------|------------|---------|--------------|
| Frametime (ms)               |            |         |              |
| Average                      | 2694.39    | 222.17  | 210.33       |
| Median                       | 2686.32    | 124.28  | 102.94       |
| Minimum                      | 1164.25    | 49.03   | 46.16        |
| Maximum                      | 4217.48    | 3896.02 | 3758.52      |
| Worst 1%                     | 4217.48    | 4285.01 | 3758.52      |
| Worst 1% (FPS)               | 0.2        | 0.3     | 0.3          |
| Worst 0.1%                   | 4217.48    | 3896.02 | 3758.52      |
| Worst 0.1% (FPS)             | 0.2        | 0.3     | 0.3          |
| Standard deviation           | 624.15     | 352.61  | 333.13       |
| CPU Usage (%)                |            |         |              |
| CPU Cores used               | 55.5       | 13.05   | 12.94        |
| Memory (Avg Mb)              |            |         |              |
| Total Allocated              | 4435       | 4536    | 4536         |
| Total Reserved               | 5008       | 5067    | 5067         |
| Managed Used                 | 769        | 769     | 769          |
| Managed Heap                 | 1272       | 1272    | 1272         |
| GFX Driver                   | 4708       | 2977    | 2977         |
| GPU frametime (ms)           |            |         |              |
| Average                      | 6.58       | 9.09    | 7.01         |
| Median                       | 8.44       | 9.66    | 7.22         |
| Maximum                      | 9.73       | 20.98   | 29.61        |
| Worst 1%                     | 9.73       | 14.51   | 12.74        |
| GPU Driver Memory (Mb)       | 5406       | 2679    | 2290         |
| Throughput (particles / avg) | 760        | 550     | 713          |


As we can see, the median frame time for LeoEcsLite is 2686.32 ms (about 0.4 FPS), while DOTS maintains a very solid 124.28 ms, even without Burst. At this point, I haven't figured out the exact reason for this behavior.

Of course, it's worth pushing the entity count even higher, especially for the Compute Shader. What about 2,000,000 entities? Well, that's still a breeze for the GPU:

| Parameter                    | ComputeShader |
|------------------------------|---------------|
| Frametime (ms)               |               |
| Average                      | 30.58         |
| Median                       | 18.89         |
| Minimum                      | 7.62          |
| Maximum                      | 3822.11       |
| Worst 1%                     | 646.33        |
| Worst 1% (FPS)               | 1.5           |
| Worst 0.1%                   | 3822.11       |
| Worst 0.1% (FPS)             | 0.3           |
| Standard deviation           | 126.19        |
| CPU Usage (%)                |               |
| CPU Cores used               | 4.2           |
| Memory (Avg Mb)              |               |
| Total Allocated              | 3943.00       |
| Total Reserved               | 4461.00       |
| Managed Used                 | 763.00        |
| Managed Heap                 | 1264.00       |
| GFX Driver                   | 2023.00       |
| GPU frametime (ms)           |               |
| Average                      | 3.85          |
| Median                       | 3.97          |
| Maximum                      | 6.67          |
| Worst 1%                     | 4.60          |
| GPU Driver Memory (Mb)       | 1722.00       |
| Throughput (particles / avg) | 519432.00     |

A median frame time of 18.89 ms translates to roughly 52 FPS. When observing the Unity Editor's stats window in real time, it showed a stable 55–60 FPS outside of the benchmark environment overhead.

## Suggestions for Improvement
I am quite sure the code is not perfect. If you find any bugs, mistakes, or possibilities for improvement, I would be very happy if you contacted me via [LinkedIn](https://www.linkedin.com/in/alexander-shnip/) or opened a GitHub issue. I truly appreciate the feedback!

## What's Next
I am also considering writing a pure CUDA and OpenGL implementation to completely eliminate any engine overhead from the benchmarks. Additionally, I have an idea for a small side project to practice using DOTS.
Stay tuned for updates!
