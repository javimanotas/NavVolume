# C# Style Guide

This document defines the coding conventions for this project.
All C# code under `Assets/` (excluding external plugins) must follow these rules.
CSharpier enforces formatting automatically — this guide covers naming, structure, and patterns that CSharpier does not catch.

---

## Table of contents

1. [Naming conventions](#naming-conventions)
2. [Fields](#fields)
3. [Properties](#properties)
4. [Methods](#methods)
5. [Custom types](#custom-types)
6. [Events](#events)
7. [Type inference](#type-inference)
8. [Object instantiation](#object-instantiation)
9. [Documentation comments](#documentation-comments)
10. [Unity-specific rules](#unity-specific-rules)

---

## Naming conventions

| Symbol | Convention | Example |
|---|---|---|
| Class | PascalCase | `PlayerController` |
| Struct | PascalCase | `DamageInfo` |
| Interface | PascalCase with `I` prefix | `IDamageable` |
| Enum | PascalCase | `EnemyState` |
| Enum value | PascalCase | `EnemyState.Patrolling` |
| Method | PascalCase | `TakeDamage()` |
| Property | PascalCase | `CurrentHealth` |
| Event | PascalCase prefixed with `On` | `OnHealthChanged` |
| Non-private field | PascalCase | `MaxHealth` |
| `[SerializeField]` field | PascalCase | `MoveSpeed` |
| Private field | camelCase with `_` prefix | `_currentHealth` |
| Local variable | camelCase | `damageAmount` |
| Parameter | camelCase | `int damageAmount` |
| Non-private constant | SCREAMING_SNAKE_CASE | `const float MAX_SPEED` |
| Private constant | SCREAMING_SNAKE_CASE with `_` prefix | `const float _MAX_SPEED` |
| Static field | PascalCase | `static int InstanceCount` |

---

## Fields

Do not use the `private` modifier.
It is the default access level and adding it results is noise:

```csharp
// Bad
private float _moveSpeed;
private Rigidbody _rb;

// Good
float _moveSpeed;
Rigidbody _rb;

// This applies to methods too.
```

Each attribute must be on its own line:

```csharp
// Bad
[SerializeField, Range(0f, 10f)] float JumpForce = 8f;

// Good
[SerializeField]
[Range(0f, 10f)]
float JumpForce = 8f;
```

Group fields by purpose, separated by a blank line:

```csharp
[SerializeField]
float MoveSpeed = 5f;

[SerializeField]
float JumpForce = 8f;

Rigidbody _rb;
Animator _animator;

bool _isGrounded;
bool _isDead;
```

---

## Properties

Use properties to expose data instead of public fields.
Prefer expression-bodied properties for simple cases:

```csharp
// Read-only backed property
public int CurrentHealth => _health;

// Read-only computed property
public bool IsAlive => _health > 0;

// Read/write with logic
public int CurrentHealth
{
    get => _health;
    private set => _health = Mathf.Clamp(value, 0, _maxHealth);
}
```

Use auto-properties only when there is no backing field logic needed:

```csharp
public string DisplayName { get; private set; }
```

---

## Methods

Prefer expression-bodied methods for single-line returns:

```csharp
// Bad
public float GetSpeedRatio()
{
    return _currentSpeed / _maxSpeed;
}

// Good
public float GetSpeedRatio() => _currentSpeed / _maxSpeed;
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
// File: PlayerController.cs
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
[DisallowMultipleComponent]
public class PlayerController : MonoBehaviour, IDamageable { }
```

### Interface implementation visibility

Prefer explicit interface implementation when the member is only intended to be called through an interface reference.
This keeps the implementing class's public surface clean and communicates that the method belongs to the contract, not the class itself.

```csharp
// Bad
public void TakeDamage(int amount)
{
    _health -= amount;
}

// Good
void IDamageable.TakeDamage(int amount)
{
    _health -= amount;
}
```

If the method also needs to be called directly on the concrete type (e.g. internally or by tightly coupled systems that already hold a typed reference), use a public method instead to avoid the awkward `((IDamageable)this).TakeDamage(...)` cast.

### Enum declarations

Declare enums in their own file when they are shared across multiple classes.

---

## Events

When subscribing to **C# events or UnityEvents** using a named method, the handler method **must start with `Handle`**.

```csharp
// Bad
_slots.OnSelectSlot += OnSlotSelected;
_button.onClick.AddListener(OnButtonClicked);

// Good
_slots.OnSelectSlot += HandleSlotSelected;
_button.onClick.AddListener(HandleButtonClicked);
```

---

## Type inference

Use `var` for all local variable declarations. Do not use explicit types for locals.

```csharp
// Bad
float speedMagnitude = 5f;
List<Enemy> enemies = new List<Enemy>();
Rigidbody rb = GetComponent<Rigidbody>();

// Good
var speedMagnitude = 5f;
var enemies = new List<Enemy>();
var rb = GetComponent<Rigidbody>();
```

This applies to all local variable contexts: assignments, `foreach` loop variables, `out` variables (e.g. `dict.TryGetValue(key, out var value)`)...

---

## Object instantiation

When constructing an object, avoid repeating the type name if it can be inferred from context.

For **field declarations**, the type is explicit on the left-hand side, so use `new()`:

```csharp
// Bad
List<Enemy> _enemies = new List<Enemy>();
Dictionary<string, int> _scores = new Dictionary<string, int>();

// Good
List<Enemy> _enemies = new();
Dictionary<string, int> _scores = new();
```

For **local variable declarations**, `var` already removes the redundancy
(see [Type inference](#type-inference)), so use `new T()` to keep the type readable:

```csharp
var enemies = new List<Enemy>();
var scores = new Dictionary<string, int>();
```

Use `new()` in method calls when the parameter type makes the constructed type unambiguous:

```csharp
// Bad
SetDamageInfo(new DamageInfo { Amount = 10, Type = DamageType.Fire });

// Good
SetDamageInfo(new() { Amount = 10, Type = DamageType.Fire });
```

---

## Documentation comments

Use `///` XML documentation comments for all public types.
For methods and properties, XML documentation is only compulsory when required to explain non-obvious behaviour, edge cases, or constraints.
Place each XML tag on its own line, separate from its content.
Place one sentence per line inside each tag.

```csharp
// Bad
/// <summary>Applies damage to the entity and triggers death if health reaches zero.</summary>
public void TakeDamage(int amount) { }

// Good
/// <summary>
/// Applies damage to the entity.
/// Triggers death if health reaches zero.
/// </summary>
/// <param name="amount">
/// The amount of damage to apply.
/// </param>
public void TakeDamage(int amount) { }
```

You do not need to document parameters or return values if their purpose is obvious from their name or type.

Omit the summary comment when the member name alone fully describes what it does, what it
returns, and any meaningful constraints — with nothing left that a comment could add.
If there is an edge case, a non-obvious invariant, or any behaviour not captured by the
name and type signature, document it:

```csharp
// No comment needed — name, return type, and backing field tell the full story
public int CurrentHealth => _health;

// Comment needed — the clamping behaviour is not obvious from the name alone
/// <summary>
/// Sets the player's health, clamped to [0, MaxHealth].
/// </summary>
public int CurrentHealth
{
    get => _health;
    private set => _health = Mathf.Clamp(value, 0, _maxHealth);
}
```

---

## Unity-specific rules

**Avoid calling `GetComponent` outside of `Awake` or `OnValidate` on your own GameObject.** Cache references at initialization to avoid repeated allocations:

```csharp
// Bad
void Update()
{
    GetComponent<Rigidbody>().AddForce(Vector3.up);
}

// Good
Rigidbody _rb;
void Awake() => _rb = GetComponent<Rigidbody>();
void Update() => _rb.AddForce(Vector3.up);
```

This rule applies to components on `this` GameObject.
Calling `GetComponent` on *other* objects is sometimes unavoidable and perfectly acceptable.

**Order Unity messages consistently** across all MonoBehaviours:

```csharp
void Awake() { }
void Start() { }
void OnEnable() { }
void OnDisable() { }
void OnDestroy() { }

// Other object methods or interface implementations here

void Update() { }
void FixedUpdate() { }
void LateUpdate() { }
```

**Subscriptions and unsubscriptions of events must be symmetric** to prevent memory leaks and duplicate subscriptions.
Use `OnEnable`/`OnDisable` for components that may be toggled, and `Awake` or `Start`/`OnDestroy` for one-time lifetime subscriptions.
