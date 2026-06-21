# NavVolume

[![Unity Tests](https://github.com/javimanotas/NavVolume/actions/workflows/unity-tests.yml/badge.svg)](https://github.com/javimanotas/NavVolume/actions/workflows/unity-tests.yml)
[![Format check](https://github.com/javimanotas/NavVolume/actions/workflows/format-check.yml/badge.svg)](https://github.com/javimanotas/NavVolume/actions/workflows/format-check.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Unity 6](https://img.shields.io/badge/Unity-6000.3-black?logo=unity)](https://unity.com/releases/editor/whats-new/6000.3)
[![Git LFS](https://img.shields.io/badge/Git%20LFS-Required-blue)](https://git-lfs.com/)

**NavVolume** is a volumetric (full 3D) pathfinding tool for Unity, built for flying agents such as drones, spaceships, birds or fish.
Unity's built-in NavMesh is surface-based and constrains agents to walkable ground.
NavVolume instead navigates the open space inside a cubic volume, so agents can move freely along all three axes.

![Tool demo](Images/title.png)

It bakes the scene geometry into a **Sparse Voxel Octree (SVO)**, searches it with a weighted **A\*** planner, and steers agents around each other and around dynamic obstacles with **ORCA** local avoidance.
The heavy work runs on background threads and Unity's Burst-compiled Job System.

## Features

- **Full 3D navigation**: agents fly through volumetric space, not over a surface.
- **Sparse Voxel Octree**: compact, hierarchical representation of free space that scales to large volumes.
- **Weighted A\* pathfinding**: choose between *Node Count* cost (keeps clear of surfaces) and *Euclidean Distance* cost (geometrically shortest), with a tunable heuristic weight to trade quality for speed.
- **Path smoothing**: greedy line-of-sight shortcutting followed by a Catmull–Rom spline for natural-looking flight.
- **ORCA local avoidance**: agents reciprocally avoid one another, dynamic obstacles, and the baked geometry.
- **Async, multithreaded**: path queries run off the main thread, baking and avoidance use the Burst-compiled Job System.
- **Bake or build at runtime**: bake the volume in the Editor for fast startup, or build it on `Awake`/on demand.
- **Editor tooling**: custom inspectors, icons and panels with time and memory stats.

## Requirements

- **Unity 6000.3** (Unity 6) or newer.
- **[Git LFS](https://git-lfs.com/)**: required to clone the repository (see below).

The package depends on the following Unity packages, all resolved automatically through the Package Manager:

- `com.unity.burst`
- `com.unity.collections`
- `com.unity.mathematics`

## Documentation

- [User Manual](Docs/user-manual.md): Quick start instructions and core types overview.
- [System Basics](Docs/system-basics.md): How the pathfinding and avoidance system works behind the scenes.

## Contributing

Contributions are welcome!
Please read [CONTRIBUTING.md](CONTRIBUTING.md) for how to set up the project, the coding and formatting conventions, and how to run the tests.
By participating you agree to abide by our [Code of Conduct](CODE_OF_CONDUCT.md).

Found a bug or have an idea? Open an [issue](https://github.com/javimanotas/NavVolume/issues/new/choose).

## License

NavVolume is released under the [MIT License](LICENSE).
You are free to use it in personal and commercial projects, however attribution by keeping the copyright notice is very much appreciated.
