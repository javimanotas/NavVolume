# User Manual

This guide describes how the tool works and how to use it from the Unity editor.
It is aimed at both technical users and level designers and artists, with the goal of making its configuration as intuitive as possible.
For the prior package installation process, see the [Installation](installation.md) guide.

## Table of contents

1. [Agents](#agents)
2. [Navigation volume](#navigation-volume)
3. [Dynamic obstacles](#dynamic-obstacles)
4. [Demo elements](#demo-elements)
5. [Using from code](#using-from-code)

---

## Agents

The first step to use the tool is to define the base configuration for an agent type.
To do this, right-click in the project window and select `Create > NavVolume > Agent Type`, as shown below.

![Creating an agent type configuration](Images/creacionagente.png)

Once this file is created, selecting it in the inspector lets you edit the following main parameters (shown below):

- **Radius:** The agent's physical radius, used to avoid collisions with both the scene geometry and other agents.
- **Cost mode:** Determines how the cost of paths is computed. With **Node Count**, the algorithm treats every node equally. This computes paths faster and makes agents tend to move through more open spaces. With **Euclidean distance**, it computes the real distance, giving more accurate results but using more performance. In very large spaces, the first method is recommended, since the final smoothing algorithm usually makes up for that loss of precision.
- **Heuristic Weight:** The weight given to the heuristic during the search. A value of 1 always guarantees the optimal path. Higher values compute the route faster, sacrificing a little accuracy in the resulting path.
- **Avoidance Neighbor Range:** The maximum distance at which other agents are taken into account when trying to avoid them.
- **Avoidance Max Neighbors:** The maximum number of agents processed at the same time for avoidance.
- **Avoidance Time Horizon Agents:** How many seconds the algorithm looks into the future to predict and avoid collisions with other agents.
- **Avoidance Time Horizon Obstacles:** How many seconds it looks into the future to avoid collisions with dynamic obstacles.

![Agent type configuration parameters](Images/configuracionagente.png)

To make a `GameObject` move autonomously, you need to add the `NavVolumeAgent` component to it and assign the configuration file you just created.
From its inspector (shown below) you can edit parameters such as:

- **Agent Type:** The agent configuration file described above.
- **Speed:** The maximum speed at which the agent will move.
- **Angular Speed:** The speed at which the agent rotates, measured in degrees per second.
- **Is Avoidance Enabled:** A checkbox to enable or disable automatic avoidance of obstacles and other characters.
- **Freeze Rotation:** By default, the agent rotates to face the direction it moves in. By checking these boxes, you can lock its rotation on specific axes. For example, if your agent is a drone, you might want it to rotate only around the Y axis.

![NavVolumeAgent component parameters](Images/componenteagente.png)

In addition, the component provides useful real-time information about its state.
As shown below, the panel displays statistics about what the agent is doing at that moment (for example, whether it is moving), how long it took to compute its last route, and more.

![Agent runtime statistics](Images/agentestats.png)

To help with visual debugging, the tool uses *Gizmos* in the scene view to draw both the raw path initially produced by the algorithm and the final, smoothed path (see below).

![Gizmos drawn by the agent](Images/gizmosagente.png)

---

## Navigation volume

For agents to move, a traversable spatial volume must exist.
To create it, add an empty object to the scene and attach the `NavVolumeSpace` component, as shown below.

![NavVolumeSpace component parameters](Images/componentevolumen.png)

The first step is to assign it an agent configuration file.
Each volume allows only one agent type, because the way the navigable space is built depends on the agent's size.
In turn, agents inside the volume are automatically assigned to it.
If several volumes of the same type overlap, the agent picks the one configured with the highest priority.

Other adjustable parameters include the total size of the volume (the side of the containing cube), the number of spatial subdivision layers, and the collision mask, which determines what physical elements of the environment act as walls or obstacles.

To make level design easier, the component includes *handles* in the scene view, shown below.
Clicking and dragging them adjusts the size of the volume directly, in a visual and intuitive way.

![Navigation volume size handles](Images/manejadores.png)

### Building the structure

To generate the internal navigation structure, the system offers three different modes:

- **Baked:** The volume is computed while editing the game and saved to a file on disk. When the game starts, this file is simply loaded, achieving the best possible performance. To use it, you first need to create a file by right-clicking in the project window (`Create > NavVolume > Baked Data`), assign it to the component, and press the *Bake* button.
- **Build On Awake:** The volume is computed automatically right when the scene starts. This is very useful if the game uses procedural generation systems where the scenario changes and cannot be precomputed.
- **Manual:** The volume is built only when you order it by calling the `Build()` function from code.

Once built, the system shows a series of statistics (shown below), such as the number of nodes per layer or the memory savings achieved compared to a traditional grid representation.
It also details how long each build phase took, from longest to shortest.
These timing figures are not saved to the file and vary between sessions, since they depend directly on the power of the computer running the process.

![Statistics of the built volume](Images/bakestats.png)

To inspect how the space was built, the tool uses *Gizmos* that draw the subdivisions.
To avoid cluttering the screen and to save performance, the smallest cubes are only drawn when the editor camera is close enough (see below).

![Gizmos drawn by the volume](Images/svogizmos.png)

---

## Dynamic obstacles

The tool allows including elements that block the way and can move during the game.
To do this, add the `NavVolumeObstacle` component to the desired object.
From this object's inspector (shown below), you can configure:

- **Shape:** The shape of the obstacle (it can be a box or a sphere).
- **Center:** The center of the figure relative to the object's original position.
- **Size:** The dimensions (width, height, and depth) if it is a box, or a single decimal value (the radius) if it is a sphere.

![NavVolumeObstacle component parameters](Images/obstaculodinamico.png)

These objects do not need physical collision components (Unity *Colliders*) for the system to detect them.
However, if they do have them, it is crucial to make sure they belong to a *Layer* that the navigation volume is ignoring in its initial collision mask.
Otherwise, the volume will think the object's original position is a permanent wall and will never allow navigation there, even if the object moves afterward.

---

## Demo elements

To make the first tests easier for new users, a utility *script* called `FlyToTarget` has been included.
This component lets agents move toward a destination without any programming.

To use it, just add it to your agent and assign a target in the `Target` field, as shown below.
The agent will automatically head toward it, and if the target changes position, the route will recalculate on its own.
Since it is a demonstration element and not part of the system core, it does not have a custom icon, unlike the rest of the tool's components.

![FlyToTarget component parameters](Images/flytotarget.png)

---

## Using from code

### `NavVolumeSpace`

- `Bounds VolumeBounds { get; }` Returns the world-space axis-aligned bounding box that delimits the volume, centered on the `transform` position and with its side equal to the configured size. It is available even before the volume is built.
- `bool IsReady { get; }` Indicates whether the navigation data has already been built or loaded and, therefore, whether the volume can answer queries and pathfinding requests. While it is `false`, the query methods return safe default values instead of throwing exceptions.
- `event Action Rebuilt` Fires on the main thread right after the volume data is (re)built. It lets you refresh anything that depends on the navigation data. Bound agents use it to automatically re-request their routes.
- `void Build()` Builds the navigation structure, replacing the previous data and then firing the `Rebuilt` event. It is used with the *Manual* build mode or to force a rebuild at runtime.
- `bool IsInsideVolume(Vector3 worldPos)` Returns `true` if the point is inside the bounding box, regardless of whether that position is free or occupied. It does not require the volume to be built.
- `bool IsNavigable(Vector3 worldPos)` Returns `true` if the point is inside the volume and the voxel that contains it is free (not blocked by geometry). Returns `false` if the volume is not ready, or if the point lies outside the volume or is blocked.
- `Vector3 ClampToVolume(Vector3 worldPos)` Returns the closest point to `worldPos` that lies inside the volume's bounding box. If it was already inside, it is returned unchanged.
- `bool TrySnapToNavigable(Vector3 worldPos, float maxDistance, out Vector3 result)` Searches for the center of the nearest free voxel to `worldPos`, exploring outward in concentric layers over the finest voxel grid, up to a maximum of `maxDistance` meters (the query point is first clamped to the inside of the volume). It returns the point found in `result` and returns `true` on success, or `false` if the volume is not ready, `maxDistance` is negative, or there is no free voxel within range.
- `Vector3 GetRandomPoint()` Returns a random navigable point inside the volume, or `Vector3.zero` if the volume is not ready or none can be found.
- `Vector3 GetRandomPointInBounds(Bounds bounds)` Like `GetRandomPoint`, but restricted to the intersection between `bounds` and the volume.
- `Vector3 GetRandomPointInSphere(Vector3 center, float radius)` Like `GetRandomPoint`, but restricted to the intersection between the given sphere and the volume.
- `bool TryGetRandomPoint(out Vector3 point, int maxAttempts = 30)` Tries to find a random navigable point inside the volume by repeatedly taking a completely random sample until one is valid or until the maximum number of attempts is reached.
- `bool TryGetRandomPointInBounds(Bounds bounds, out Vector3 point, int maxAttempts = 30)` Same as `TryGetRandomPoint`, sampling within the intersection between `bounds` and the volume.
- `bool TryGetRandomPointInSphere(Vector3 center, float radius, out Vector3 point, int maxAttempts = 30)` Same as `TryGetRandomPoint`, sampling within the intersection between the sphere and the volume.

### `NavVolumeAgent`

- `NavVolumeSpace NavVolumeSpace { get; }` The volume the agent is currently bound to, resolved automatically at startup.
- `void SetDestination(Vector3 goal)` Requests a route from the agent's current position to `goal` and starts flying as soon as it is found. The search runs in the background and, if called again, cancels any in-flight request. The destination is remembered, so the agent automatically recomputes its route if its volume is rebuilt. The destination should be a navigable point. If unsure, it is best to snap it first with `TrySnapToNavigable`.
