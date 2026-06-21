# C# Style Guide

This document defines the coding conventions for this project.
All C# code under `Assets/` (excluding external plugins) must follow these rules.

## Table of contents

1. [Formatting](#formatting)
2. [Naming conventions](#naming-conventions)
3. [Namespaces](#namespaces)
4. [Numeric literals](#numeric-literals)
5. [Custom types](#custom-types)
6. [Documentation comments](#documentation-comments)
7. [Logging](#logging)

---

## Formatting

This project uses **CSharpier** to enforce consistent C# formatting. A CI check runs automatically targeting `main`.
If your code is not formatted correctly the check will fail.

### Install

You need the .NET SDK installed first. Then run:

```bash
dotnet tool install csharpier --global
```

### Using CSharpier

Format your code before committing:

```bash
csharpier format Assets
```

If you want to see what would fail before committing, run CSharpier in check mode:

```bash
csharpier check Assets
```

If you use Visual Studio you can download the extension. That allows to format automatically on save.

---

## Naming conventions

| Symbol | Convention | Example |
|---|---|---|
| Class | PascalCase | `NavVolumeSpace` |
| Struct | PascalCase | `PathRequest` |
| Interface | PascalCase with `I` prefix | `IHeuristic` |
| Enum | PascalCase | `PathStatus` |
| Enum value | PascalCase | `PathStatus.Success` |
| Method | PascalCase | `FindPath()` |
| Property | PascalCase | `CurrentNode` |
| Event | PascalCase prefixed with `On` | `OnPathCalculated` |
| Non-private field | PascalCase | `VoxelSize` |
| Private field | camelCase with `_` prefix | `_voxelSize` |
| Non-private static field | PascalCase | `InstanceCount` |
| Private static field | PascalCase with `s_` prefix | `s_InstanceCount` |
| Local variable | camelCase | `searchDepth` |
| Parameter | camelCase | `searchDepth` |
| Boolean | Prefixed with `is`, `has`, `can`, `should`, etc. (casing adapts to the symbol type) | `_isWalkable`, `IsWalkable`, `isWalkable` |
| Non-private constant | SCREAMING_SNAKE_CASE | `const float MAX_DISTANCE` |
| Private constant | SCREAMING_SNAKE_CASE with `_` prefix | `const float _MAX_DISTANCE` |
| Generic type parameter | PascalCase with `T` prefix | `TKey`, `TValue`, `T` |
| Goto labels | PascalCase | `goto BreakNestedLoops;` |
| Namespace | PascalCase segments separated by `.` | `NavVolume.Runtime.Pathfinding` |
| Region label | Sentence case | `#region Pathfinding configuration` |

### Boolean casing

The boolean prefix (`is`, `has`, `can`, `should`, etc.) must follow the casing convention of the symbol it belongs to.
Do not apply a fixed casing to the prefix itself, let the symbol type dictate it:

```csharp
// Private field
bool _isWalkable;
bool _hasNeighbors;

// Public property
public bool IsLeaf => _children == null;
public bool CanTraverse { get; private set; }

// Local variable or parameter
var isValid = CheckValidity();
void SetActive(bool isActive) { }
```

---

## Namespaces

Namespaces must be relative to the root of the assembly definition (`.asmdef`) the file belongs to.
Optionally, append the folder path relative to that asmdef root to further organise sub-systems.

```csharp
// asmdef root namespace: NavVolume.Runtime
// File at the asmdef root:
namespace NavVolume.Runtime

// File inside a subfolder:
namespace NavVolume.Runtime.Pathfinding
```

**Exception:** If you want certain types to be on the `prelude` the namespace should be the project name: `NavVolume`, regardless of folder structure.

---

## Numeric literals

Write numeric suffixes and the `0x` hexadecimal prefix in lowercase.
Hexadecimal digits `A`-`F` must be uppercase:

```csharp
// Bad
var voxelSize = 5F;
var mask = 0XFF00A3u;
var count = 10U;
var big = 9999L;

// Good
var voxelSize = 5f;
var mask = 0xFF00A3u;
var count = 10u;
var big = 9999l;
```

---

## Custom types

Declare only one type per file.
The file name must match the type name exactly.

### Classes that inherit from MonoBehaviour

Always specify `RequireComponent` for any component your class depends on.
Add `DisallowMultipleComponent` whenever it makes no sense to have duplicates.
Each attribute goes on its own line:

```csharp
[RequireComponent(typeof(NavVolumeSpace))]
[RequireComponent(typeof(Animator))]
[DisallowMultipleComponent]
public class NavVolumeAgent : MonoBehaviour, IPathfinder { }
```

---

## Documentation comments

Use `///` XML documentation comments for all public types.
For methods and properties, XML documentation is only compulsory when required to explain non-obvious behaviour, edge cases, or constraints.
Place each XML tag on its own line, separate from its content.
Place one sentence per line inside each tag.

```csharp
// Bad
/// <summary>Calculates a path to the target and returns the result.</summary>
public void RequestPath(Vector3 target) { }

// Good
/// <summary>
/// Calculates a path to the given target.
/// Returns a partial path if the target is unreachable.
/// </summary>
/// <param name="target">
/// The destination position in world space.
/// </param>
public void RequestPath(Vector3 target) { }
```

You do not need to document parameters or return values if their purpose is obvious from their name or type.

Omit the summary comment when the member name alone fully describes what it does, what it returns, and any meaningful constraints, with nothing left that a comment could add.
If there is an edge case, a non-obvious invariant, or any behaviour not captured by the name and type signature, document it:

```csharp
// No comment needed: name, return type, and backing field tell the full story
public int CurrentDepth => _depth;

// Comment needed: the clamping behaviour is not obvious from the name alone
/// <summary>
/// Sets the search depth, clamped to [0, MaxSearchNodes].
/// </summary>
public int CurrentDepth
{
    get => _depth;
    private set => _depth = Mathf.Clamp(value, 0, _maxSearchNodes);
}
```

### Lists

When a summary contains a list, each `<item>` tag must be placed on the same line as its content and indented with one tab inside the `<list>` block:

```csharp
// Bad
/// <summary>
/// Supported path status:
/// <list type="bullet">
/// <item>
/// Success: a complete path was found.
/// </item>
/// <item>
/// Partial: only a partial path was found.
/// </item>
/// </list>
/// </summary>

// Good
/// <summary>
/// Supported path status:
/// <list type="bullet">
///     <item>Success: a complete path was found.</item>
///     <item>Partial: only a partial path was found.</item>
///     <item>Failed: no path could be calculated.</item>
/// </list>
/// </summary>
```

---

## Logging

Always prefix log messages with `[NavVolume]` so users of the tool can easily identify and filter NavVolume output in the Unity Console.
Follow it immediately with `[ComponentName]` (the class or system producing the log) and a space before the message:

```csharp
// Bad
Debug.Log("Volume baked.");
Debug.LogWarning("Target out of bounds.");
Debug.LogError("Path not found.");

// Good
Debug.Log("[NavVolume][SVOBuilder] Volume baked.");
Debug.LogWarning("[NavVolume][NavVolumeAgent] Target out of bounds.");
Debug.LogError("[NavVolume][SVOPathfinder] Path not found.");
```

The component tag should match the class name exactly.
For static utility classes or systems without a MonoBehaviour, use the class name:

```csharp
Debug.Log("[NavVolume][MortonCode] Conversion completed in 42ms.");
```
