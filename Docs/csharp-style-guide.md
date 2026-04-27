# C# Style Guide

This document defines the coding conventions for this project.
All C# code under `Assets/` (excluding external plugins) must follow these rules.

## Table of contents

1. [Formatting](#formatting)
2. [Naming conventions](#naming-conventions)
3. [Braces](#braces)
4. [Namespaces](#namespaces)
5. [Regions](#regions)
6. [Numeric literals](#numeric-literals)
7. [Fields](#fields)
8. [Properties](#properties)
9. [Methods](#methods)
10. [Custom types](#custom-types)
11. [Events](#events)
12. [Type inference](#type-inference)
13. [Object instantiation](#object-instantiation)
14. [Documentation comments](#documentation-comments)
15. [Logging](#logging)
16. [Unity-specific rules](#unity-specific-rules)

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

## Braces

Always use braces for `if`, `else`, and loop bodies, even for single-line statements.
This prevents subtle bugs when lines are added later and keeps diffs clean.
The only exception is lambda expressions:

```csharp
// Bad
if (!node.IsLeaf)
    ProcessChildren();

if (!node.IsLeaf)
    ProcessChildren();
else
    ProcessLeaf();

foreach (var neighbor in neighbors)
    Process(neighbor);

// Good
if (!node.IsLeaf)
{
    ProcessChildren();
}

if (!node.IsLeaf)
{
    ProcessChildren();
}
else
{
    ProcessLeaf();
}

foreach (var neighbor in neighbors)
{
    Process(neighbor);
}

// Exception: lambdas do not require braces
var walkable = nodes.Where(n => n.IsWalkable);
requests.ForEach(r => r.Cancel());
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

## Regions

Use `#region` to group related members or logic, helping to explain briefly what a chunk of code does.

### Grouping fields and functions

You can use regions to group multiple fields that share a relationship, or functions that are closely related.
This improves readability and provides a quick overview of the code's structure:

```csharp
#region Pathfinding Configuration

[SerializeField]
float _agentRadius = 0.5f;

[SerializeField]
int _maxSearchNodes = 1000;

#endregion

#region Path Processing

void ProcessPathRequest(PathRequest request) { }

void OnPathComplete(PathResult result) { }

#endregion
```

### Annotating complex method bodies

Regions can also be used inside functions to group parts of the code.
They act as logical labels to briefly explain what a chunk of code does within a longer method:

```csharp
void BakeVolume()
{
    #region Rasterize Geometry
    // ...complex rasterization logic...
    #endregion

    #region Build Sparse Voxel Octree
    // ...tree construction code...
    #endregion

    #region Link Neighbors
    // ...adjacency linking logic...
    #endregion
}
```

Keep in mind that it is always better to split the code into several functions.
This applies only if it's very difficult to refactor into separate functions.

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

## Fields

Do not use the `private` modifier.
It is the default access level and adding it results is noise:

```csharp
// Bad
private float _voxelSize;
private NavVolumeSpace _volume;

// Good
float _voxelSize;
NavVolumeSpace _volume;

// This applies to methods too.
```

Each attribute must be on its own line:

```csharp
// Bad
[SerializeField, Range(0.1f, 10f)] float _agentRadius = 0.5f;

// Good
[SerializeField]
[Range(0.1f, 10f)]
float _agentRadius = 0.5f;
```

Group fields by purpose, separated by a blank line:

```csharp
[SerializeField]
float _agentRadius = 0.5f;

[SerializeField]
int _maxSearchNodes = 1000;

NavVolumeSpace _volume;
SVOPathfinder _pathfinder;

bool _isBaking;
bool _hasVolume;
```

---

## Properties

Use properties to expose data instead of public fields.
Prefer expression-bodied properties for simple cases:

```csharp
// Read-only backed property
public int CurrentDepth => _depth;

// Read-only computed property
public bool IsWalkable => _walkableFlag > 0;

// Read/write with logic
public int CurrentDepth
{
    get => _depth;
    private set => _depth = Mathf.Clamp(value, 0, _maxDepth);
}

// Different encapsulation for get and set
[field: SerializeField]
public UnityEvent<PathResult> OnPathCalculated { get; private set; }
```

Use auto-properties only when there is no backing field logic needed:

```csharp
public MortonCode NodeCode { get; private set; }
```

---

## Methods

Prefer expression-bodied methods for single-line returns:

```csharp
// Bad
public float GetHeuristicCost()
{
    return _distance / _maxDistance;
}

// Good
public float GetHeuristicCost() => _distance / _maxDistance;
```

### Test method naming

In test assemblies, method names may use `_` as a word separator to improve readability.
Long descriptive names are encouraged; prefer the pattern `MethodUnderTest_Condition_ExpectedResult`:

```csharp
[Test]
public void FindPath_WhenDestinationUnreachable_ShouldReturnFailed() { }

[Test]
public void InsertNode_WithValidMortonCode_ShouldUpdateTree() { }
```

This exception applies only to test methods and does not extend to helpers or setup methods within the same file.

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

### Interface implementation visibility

Prefer explicit interface implementation when the member is only intended to be called through an interface reference.
This keeps the implementing class's public surface clean and communicates that the method belongs to the contract, not the class itself.

```csharp
// Bad
public void RequestPath(Vector3 target)
{
    _target = target;
}

// Good
void IPathfinder.RequestPath(Vector3 target)
{
    _target = target;
}
```

If the method also needs to be called directly on the concrete type (e.g. internally or by tightly coupled systems that already hold a typed reference), use a public method instead to avoid the awkward `((IPathfinder)this).RequestPath(...)` cast.

---

## Events

When subscribing to **C# events or UnityEvents** using a named method, the handler method **must start with `Handle`**.

```csharp
// Bad
_agent.OnPathRequested += OnPathRequested;
_button.onClick.AddListener(OnBakeClicked);

// Good
_agent.OnPathRequested += HandlePathRequested;
_button.onClick.AddListener(HandleBakeClicked);
```

---

## Type inference

Use `var` for all local variable declarations. Do not use explicit types for locals.
The only exception is where using var instead of the concrete type would require to do a cast.

```csharp
// Bad
float distance = 5f;
List<SVONode> nodes = new List<SVONode>();
NavVolumeSpace volume = GetComponent<NavVolumeSpace>();

// Good
var distance = 5f;
var nodes = new List<SVONode>();
var volume = GetComponent<NavVolumeSpace>();
```

This applies to all local variable contexts: assignments, `foreach` loop variables, `out` variables (e.g. `dict.TryGetValue(key, out var value)`)...

---

## Object instantiation

When constructing an object, avoid repeating the type name if it can be inferred from context.

For **field declarations**, the type is explicit on the left-hand side, so use `new()`:

```csharp
// Bad
List<SVONode> _nodes = new List<SVONode>();
Dictionary<MortonCode, SVONode> _nodeMap = new Dictionary<MortonCode, SVONode>();

// Good
List<SVONode> _nodes = new();
Dictionary<MortonCode, SVONode> _nodeMap = new();
```

For **local variable declarations**, `var` already removes the redundancy
(see [Type inference](#type-inference)), so use `new T()` to keep the type readable:

```csharp
var nodes = new List<SVONode>();
var nodeMap = new Dictionary<MortonCode, SVONode>();
```

Use `new()` in method calls when the parameter type makes the constructed type unambiguous:

```csharp
// Bad
SetPathRequest(new PathRequest { Target = destination, MaxDepth = 10 });

// Good
SetPathRequest(new() { Target = destination, MaxDepth = 10 });
```

In case the type was not clear you can declare it in a variable above.

```csharp
var request = new PathRequest { Target = destination, MaxDepth = 10 };
SetPathRequest(request);
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

---

## Unity-specific rules

**Avoid calling `GetComponent` outside of `Awake` or `OnValidate` on your own GameObject.** Cache references at initialization to avoid repeated allocations:

```csharp
// Bad
void Update()
{
    GetComponent<NavVolumeSpace>().RequestBake();
}

// Good
NavVolumeSpace _volume;
void Awake() => _volume = GetComponent<NavVolumeSpace>();
void Update() => _volume.RequestBake();
```

This rule applies to components on `this` GameObject.
Calling `GetComponent` on *other* objects or in parent is sometimes unavoidable and perfectly acceptable.

**Order Unity messages consistently** across all MonoBehaviours:

```csharp
// Methods that are called eventually:
void Awake() { }
void Start() { }
void OnEnable() { }
void OnDisable() { }
void OnDestroy() { }
// OnTriggerEnter, OnCollisionEnter...

// Other object methods or interface implementations here

// Methods that are called periodically:
void Update() { }
void FixedUpdate() { }
void LateUpdate() { }
void OnDrawGizmos() { }
void OnDrawGizmosSelected() { }
```

Auxiliary methods can be placed below.

**Subscriptions and unsubscriptions of events must be symmetric** to prevent memory leaks and duplicate subscriptions.
Use `OnEnable`/`OnDisable` for components that may be toggled, and `Awake` or `Start`/`OnDestroy` for one-time lifetime subscriptions.
