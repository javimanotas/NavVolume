# C# Style Guide

This document defines the coding conventions for this project.
All C# code under `Assets/` (excluding external plugins) must follow these rules.
CSharpier enforces formatting automatically — this guide covers naming, structure, and patterns that CSharpier does not catch.

---

## Table of contents

1. [Naming conventions](#naming-conventions)
2. [Variables and fields](#variables-and-fields)
3. [Properties](#properties)
4. [Methods](#methods)
5. [Classes, structs and interfaces](#classes-structs-and-interfaces)
6. [Enums](#enums)
7. [Lambdas and delegates](#lambdas-and-delegates)
8. [Type inference](#type-inference)
9. [Object instantiation](#object-instantiation)
10. [Documentation comments](#documentation-comments)
11. [Namespaces](#namespaces)
12. [Unity-specific rules](#unity-specific-rules)

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
| `[SerializeField]` field | PascalCase (see [Variables and fields](#variables-and-fields)) | `MoveSpeed` |
| Private field | camelCase with `_` prefix | `_currentHealth` |
| Local variable | camelCase | `damageAmount` |
| Parameter | camelCase | `int damageAmount` |
| Non-private constant | SCREAMING_SNAKE_CASE | `const float MAX_SPEED` |
| Private constant | SCREAMING_SNAKE_CASE with `_` prefix | `const float _MAX_SPEED` |
| Static field | PascalCase | `static int InstanceCount` |

---

## Variables and fields

Do not write the `private` modifier — it is the default access level and adding it is noise:

```csharp
// Bad
private float _moveSpeed;
private Rigidbody _rb;

// Good
float _moveSpeed;
Rigidbody _rb;
```

Private fields use a leading underscore:

```csharp
float _moveSpeed;
Rigidbody _rb;
bool _isGrounded;
```

`[SerializeField]` fields are private but use PascalCase to match how Unity displays them in the Inspector.
Each attribute must be on its own line:

```csharp
// Bad
[SerializeField] float _moveSpeed = 5f;
[SerializeField, Range(0f, 10f)] float _jumpForce = 8f;

// Good
[SerializeField]
float MoveSpeed = 5f;

[SerializeField]
[Range(0f, 10f)]
float JumpForce = 8f;
```

Constants use SCREAMING_SNAKE_CASE. Private constants follow the same underscore prefix rule as private fields:

```csharp
const float MAX_SPEED = 10f;
const int MAX_HEALTH = 100;

const float _GRAVITY_MULTIPLIER = 2.5f;
const string _GROUND_TAG = "Ground";
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
// Read-only computed property
public bool IsAlive => _health > 0;

// Read-only backed property
public int CurrentHealth => _health;

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

## Classes, structs and interfaces

One type per file. The file name must match the type name exactly.

Always specify `RequireComponent` for any component your class depends on,
and `DisallowMultipleComponent` whenever it makes no sense to have duplicates.
Each attribute goes on its own line:

```csharp
// File: PlayerController.cs
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
[DisallowMultipleComponent]
public class PlayerController : MonoBehaviour, IDamageable
{
}
```

`RequireComponent` prevents missing reference errors at edit time and documents dependencies explicitly.

Apply `DisallowMultipleComponent` to every `MonoBehaviour` by default. Omit it only when the class is
explicitly designed to coexist in multiples on the same `GameObject`, and document that intent with
a comment directly above the class declaration.

### Interface implementation visibility

When implementing an interface, **prefer explicit interface implementation** instead of public methods or properties.

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

---

## Enums

Declare enums in their own file when they are shared across multiple classes.

Do not use a suffix or prefix on enum values:

```csharp
// Bad
public enum Direction { Direction_Up, Direction_Down }

// Good
public enum Direction { Up, Down, Left, Right }
```

---

## Lambdas and delegates

Use expression-bodied lambdas when the body is a single statement:

```csharp
_enemies.ForEach(enemy => enemy.TakeDamage(10));

var alive = _enemies.Where(enemy => enemy.IsAlive).ToList();
```

Use `Action` and `Func` for delegates instead of declaring custom delegate types:

```csharp
public event Action OnDeath;
public event Action<int> OnHealthChanged;
public event Func<bool> OnCanInteract;
```

### Event handler naming

When subscribing to **C# events or UnityEvents** using a **named method** (not a lambda),
the handler method **must start with `Handle`**.

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
`var` does not apply to field declarations — fields must always use an explicit type.

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

This applies to all local variable contexts: assignments, `foreach` loop variables,
and `out` variables (e.g. `dict.TryGetValue(key, out var value)`).

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

Use `///` XML documentation comments for all public types (classes, structs, interfaces, enums).
For methods and properties, XML documentation is only compulsory when required to explain non-obvious behaviour, edge cases, or constraints.
Place each XML tag on its own line, separate from its content.
Place one sentence per line inside `<summary>`.

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

Document parameters and return values separately, but only when they are non-trivial or require further explanation. You do not need to document parameters or return values if their purpose is obvious from their name or type:

```csharp
/// <summary>
/// Calculates the damage after applying resistance.
/// Returns zero if the entity is invulnerable.
/// </summary>
/// <param name="rawDamage">
/// The incoming damage before resistance is applied.
/// </param>
/// <param name="resistance">
/// A value between 0 and 1 where 1 means full immunity.
/// </param>
/// <returns>
/// The final damage value after resistance calculation.
/// </returns>
public int CalculateDamage(int rawDamage, float resistance) =>
    Mathf.RoundToInt(rawDamage * (1f - resistance));
```

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

## Namespaces

Always declare namespaces for your types. The namespace must match the full folder path starting from the root directory, using `Assets.Scripts` as the base. For instance, a script located at `Assets/Scripts/Player/ThirdPersonController.cs` must be placed in the `Assets.Scripts.Player` namespace.

**Exception for Utilities:**
Files located within the `Utils` directory (e.g., `Assets/Scripts/Utils/...`) should **not** declare a namespace. Omitting the namespace for utility classes, generic helpers, and extension methods ensures they are globally accessible across the entire project without requiring repetitive `using` directives in every file.

---

## Unity-specific rules

**Never call `GetComponent` outside of `Awake` or `OnValidate`.** Cache references at initialization:

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

**Never use `Camera.main` in `Update`.** It does an internal `FindObjectWithTag` every call:

```csharp
// Bad
void Update()
{
    var dir = Camera.main.transform.forward;
}

// Good
Camera _camera;
void Awake() => _camera = Camera.main;
void Update()
{
    var dir = _camera.transform.forward;
}
```

**Never use `new` to create a `MonoBehaviour`.** Always use `AddComponent` or `Instantiate`:

```csharp
// Bad
var player = new PlayerController();

// Good
var player = gameObject.AddComponent<PlayerController>();
```

**Unsubscribe from events in `OnDisable` or `OnDestroy`** to prevent memory leaks.
Store the handler in a field so the subscription and unsubscription reference the same delegate instance.
Unsubscribing a lambda that was not stored will silently fail because each lambda expression
creates a new delegate instance:

```csharp
// Bad — unsubscription silently fails; each lambda is a distinct instance
void OnEnable()  => _health.OnChanged += value => UpdateUI(value);
void OnDisable() => _health.OnChanged -= value => UpdateUI(value);

// Good — both lines reference the same stored delegate
void OnEnable()  => _health.OnChanged += HandleHealthChanged;
void OnDisable() => _health.OnChanged -= HandleHealthChanged;

void HandleHealthChanged(int value) => UpdateUI(value);
```

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
