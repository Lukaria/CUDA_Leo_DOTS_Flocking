# Murmuration Simulation

A [murmuration](https://uncertainty.club/murmuration/) (flocking) simulation of particles implemented using three different approaches: **LeoEcsLite with Threads**, **Unity DOTS stack**, and **Compute Shaders**.

<p align="center">
    <img src="https://github.com/Lukaria/CUDA_Leo_DOTS_Flocking/blob/main/media/murmuration.gif" width="25%" />
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
How do we effectively find all neighbors? The first thing that comes to mind is using a Hash Map to store boid IDs. We could hash the particle position and store its ID in a bucket.

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

There is [great scientific atricle](https://dl.acm.org/doi/epdf/10.1145/1658866.1658870) about boids and small description of such spatial hash approach

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

2. **[Unity DOTS](https://github.com/Lukaria/CUDA_Leo_DOTS_Flocking/tree/main/DOTS))**

The second approach uses the Unity DOTS stack (Jobs, Burst Compiler, Entities, and Entities Graphics), which allows for highly optimized multi-threaded code. It offers many useful tools aimed at simplifying ECS-style development and achieving high performance.

3. **[Compute Shaders](https://github.com/Lukaria/CUDA_Leo_DOTS_Flocking/tree/main/ComputeShaders)**

The third approach uses Compute Shaders running entirely on the GPU. By using a ComputeBuffer to store data on the GPU, we can perform massive parallel computations with zero CPU-to-GPU transfer overhead. Rendering is handled via a single DrawProcedural call.

**Note:** Naturally, none of these approaches use standard Unity Physics, ensuring maximum performance.

## Benchmarks
(Note: Add benchmark table here. Keep in mind the Unity Editor overhead).

## Suggestions for Improvement
I am quite sure the code is not perfect. If you find any bugs, mistakes, or possibilities for improvement, I would be very happy if you contacted me via [LinkedIn](https://www.linkedin.com/in/alexander-shnip/) or opened a GitHub issue. I truly appreciate the feedback!

## What's Next
I am currently working on a CUDA + OpenGL implementation to benchmark these algorithms without any Game Engine overhead. Stay tuned for updates!
