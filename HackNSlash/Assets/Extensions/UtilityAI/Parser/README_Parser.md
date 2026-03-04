# AI Definition Language — Parser System

`Extensions/UtilityAI/Parser/`

Generic, compiler-theory-grade parser that converts a plain text file into Unity
ScriptableObject assets for the utility AI system. Works in both directions:
text → assets (import) and assets → text (export).

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Import Behaviour — Overwriting](#import-behaviour)
3. [The Grammar (Language Reference)](#the-grammar)
4. [Consideration Types](#consideration-types)
5. [DamageableComponent Consideration](#damageablecomponent-consideration)
6. [Extending the System](#extending-the-system)
7. [Adding a New Action Generator](#adding-a-new-action-generator)

---

## Architecture Overview

The pipeline follows classical compiler theory with three distinct passes:

```
Source text
    │
    ▼ Lexer          (Lexer.cs)
List<Token>
    │
    ▼ AiDefParser    (AiDefParser.cs)
DocumentNode (AST)
    │
    ▼ AiDefCodeGenerator + IActionCodeGen  (CodeGen.cs / AiDefCodeGenerator.cs)
ScriptableObject assets on disk
```

### Lexer (`Lexer.cs`)
Converts raw source characters into a flat `List<Token>`. Handles:
- Line comments `//` and `#`
- Block comments `/* ... */`
- Quoted strings `"..."` and `'...'`
- Integer and floating-point numbers (including negatives)
- Boolean literals `true` / `false`
- Identifiers (letters, digits, underscores, dots)
- All structural punctuation `{ } ( ) [ ] : , = @`

### Parser (`AiDefParser.cs`)
Recursive-descent parser. Consumes the token list and builds an AST
(`DocumentNode` → `ActionDefNode` → `ConsiderationNode` → ...).
Throws `ParseException` with line numbers on syntax errors.

### AST Nodes (`AstNodes.cs`)
Plain C# classes. Every node carries a `Line` field for error reporting.

### Code Generator (`CodeGen.cs`, `AiDefCodeGenerator.cs`)
- `IActionCodeGen` — one implementation per action type keyword.
- `ConsiderationBuilder` — maps consideration AST nodes to concrete instances.
- `AiDefCodeGenerator` — orchestrator: runs the full import pipeline and serialises exports.

---

## Import Behaviour

On import, if an asset already exists at the computed output path, the importer
calls `EditorUtility.CopySerialized` to overwrite it in-place rather than creating
a duplicate. This means:

- **No duplicate assets** when reimporting.
- **All scene references are preserved** — if your `EnemyStateMachine` has the asset
  in its actions list, that slot updates automatically.
- Name changes in the `.aidef` file will create a new asset and leave the old one in place.
  Delete the old asset manually in that case.

---

## The Grammar

```
document     = action* EOF

action       = 'action' STRING ':' IDENT '{' action_body '}'
action_body  = (property | 'consideration' consideration_block)*

property     = IDENT '=' value

value        = STRING | NUMBER | BOOL | IDENT | array
array        = '[' (value (',' value)*)? ']'

consideration_block = IDENT '{' property* (composite_extras)? '}'
```

**Comments:** `// line comment`, `# line comment`, `/* block comment */`

---

## Consideration Types

All consideration blocks follow the pattern:
```
consideration <type> {
  <properties>
}
```

### `constant`
```
consideration constant { value = 0.5 }
```
`value` accepts any non-negative float — it is **not** clamped to [0,1].
A value like `10` is used for "guaranteed win" override actions (e.g. shield-break stun).

### `random`
```
consideration random { min = 0.0  max = 1.0 }
```

### `curve`
```
consideration curve {
  key    = "float.dist_to_player_norm"
  points = [[0.0, 1.0], [0.5, 0.8], [1.0, 0.0]]
}
```

### `inrange`
```
consideration inrange {
  key      = "target.player"
  maxdist  = 5.0
  maxangle = 90.0
  points   = [[0.0, 1.0], [1.0, 0.0]]
}
```

### `bool`
```
consideration bool {
  key    = "bool.is_shielded"
  invert = false
}
```

### `targetexists`
```
consideration targetexists { key = "target.player" }
```

### `threshold`
```
consideration threshold {
  key        = "float.self_health_norm"
  comparison = lessthan        # greaterthan | lessthan | greaterorequal | lessorequal
  threshold  = 0.3
  iftrue     = 1.0
  iffalse    = 0.0
}
```

### `stringmatch`
```
consideration stringmatch {
  key      = "history.last_action_name"
  match    = "EnemySweepAttack"
  ifmatch   = 1.0
  ifnomatch = 0.0
}
```

### `composite`
```
consideration composite {
  allMustBeNonZero = true
  first targetexists { key = "target.player" }
  rest [
    multiply: curve { key = "float.dist_to_player_norm"  points = [[0,1],[1,0]] },
    multiply: threshold { ... }
  ]
}
```
Operations: `multiply`, `average`, `add`, `min`, `max`.

---

## DamageableComponent Consideration

`DamageableComponentConsideration` queries any `IDamageableComponent` on the brain owner
directly by calling its `Evaluate()` method, then optionally passes the result through a
curve and/or invert.

This consideration is **inspector-only** — it is not parseable from text because the
component type is selected via a type-picker dropdown (`[DamageableComponentType]` attribute),
which stores an assembly-qualified name. Use context keys (e.g. `bool.is_shielded`) in
`.aidef` files instead.

To implement a new component for this consideration:
1. Create a class implementing `IDamageableComponent`.
2. Implement `Evaluate() : float` — return [0,1] representing the component's current state.
3. Register it with the owning entity's `DamageableComponents` entity during setup.
4. It will automatically appear in the dropdown.

---

## Extending the System

### Adding a new consideration type
1. Create a `[Serializable]` class inheriting `Consideration` in `ConsiderationBases`.
2. Add a `case` to `ConsiderationBuilder.Build` in `CodeGen.cs`.
3. Add a matching `ParseXxx(int line)` method and case in `AiDefParser.ParseConsideration`.
4. Add the matching serialise case in `AiDefCodeGenerator.SerialiseConsideration`.

---

## Adding a New Action Generator

1. Create a class implementing `IActionCodeGen`.
2. Register it via `generator.RegisterActionGen(new YourActionCodeGen())`.
   For enemy actions, do this in `EnemyAiCodeGeneratorFactory.Create()`.

Existing `.aidef` files are unaffected.


`Extensions/UtilityAI/Parser/`

Generic, compiler-theory-grade parser that converts a plain text file into Unity
ScriptableObject assets for the utility AI system. Works in both directions:
text → assets (import) and assets → text (export).

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [The Grammar (Language Reference)](#the-grammar)
3. [Consideration Types](#consideration-types)
4. [Extending the System](#extending-the-system)
5. [Adding a New Action Generator](#adding-a-new-action-generator)

---

## Architecture Overview

The pipeline follows classical compiler theory with three distinct passes:

```
Source text
    │
    ▼ Lexer          (AstNodes.cs / Lexer.cs)
List<Token>
    │
    ▼ AiDefParser    (AiDefParser.cs)
DocumentNode (AST)
    │
    ▼ AiDefCodeGenerator + IActionCodeGen  (CodeGen.cs / AiDefCodeGenerator.cs)
ScriptableObject assets on disk
```

### Lexer (`Lexer.cs`)
Converts raw source characters into a flat `List<Token>`. Handles:
- Line comments `//` and `#`
- Block comments `/* ... */`
- Quoted strings `"..."` and `'...'`
- Integer and floating-point numbers (including negatives)
- Boolean literals `true` / `false`
- Identifiers (letters, digits, underscores, dots)
- All structural punctuation `{ } ( ) [ ] : , = @`

### Parser (`AiDefParser.cs`)
Recursive-descent parser. Consumes the token list and builds an AST
(`DocumentNode` → `ActionDefNode` → `ConsiderationNode` → ...).
Throws `ParseException` with line numbers on syntax errors.

### AST Nodes (`AstNodes.cs`)
Plain C# classes. Every node carries a `Line` field for error reporting.
No init-only properties — all fields are public so object-initializer syntax
works with any Unity-supported C# version.

### Code Generator (`CodeGen.cs`, `AiDefCodeGenerator.cs`)
- `IActionCodeGen` — one implementation per action type keyword.
  Handles both `Generate` (AST node → SO) and `Serialise` (SO → text).
- `ConsiderationBuilder` — maps `ConsiderationNode` AST subtrees to
  concrete `Consideration` instances. Fully generic — knows nothing about
  the specific context key registry.
- `AiDefCodeGenerator` — the orchestrator. Holds a registry of generators,
  runs the full import pipeline, and serialises exports.

---

## The Grammar

```
document     = action* EOF

action       = 'action' STRING ':' IDENT '{' action_body '}'
action_body  = (property | 'consideration' consideration_block)*

property     = IDENT '=' value

value        = STRING | NUMBER | BOOL | IDENT | array
array        = '[' (value (',' value)*)? ']'

consideration_block = IDENT '{' property* (composite_extras)? '}'

# composite extras inside a composite block:
composite_extras = 'first' consideration_block
                   ('rest' '[' (IDENT ':' consideration_block (',' ...)?)* ']')?
```

**Comments:** `// line comment`, `# line comment`, `/* block comment */`

**String literals:** `"text"` or `'text'` — use for action names and context key IDs.

**Identifiers:** Used for action type keywords, strategy names, property keys,
consideration type names, and aggregation operators. Case-insensitive everywhere.

---

## Consideration Types

All consideration blocks follow the pattern:
```
consideration <type> {
  <properties>
}
```

### `constant`
```
consideration constant { value = 0.5 }
```
| Property | Type  | Default | Description |
|----------|-------|---------|-------------|
| `value`  | float | 0.5     | Fixed utility score [0,1]. |

### `random`
```
consideration random { min = 0.0  max = 1.0 }
```

### `curve`
```
consideration curve {
  key    = "float.dist_to_player_norm"
  points = [[0.0, 1.0], [0.5, 0.8], [1.0, 0.0]]
}
```
`key` is the `ContextKey.InternalId` of a float context key.
`points` is an array of `[time, value]` pairs that define the `AnimationCurve`.

### `inrange`
```
consideration inrange {
  key      = "target.player"
  maxdist  = 5.0
  maxangle = 90.0
  points   = [[0.0, 1.0], [1.0, 0.0]]
}
```
`key` must be a `ContextKey<Transform>` internal ID.

### `bool`
```
consideration bool {
  key    = "bool.is_aggro"
  invert = false
}
```

### `targetexists`
```
consideration targetexists { key = "target.player" }
```

### `threshold`
```
consideration threshold {
  key        = "float.self_health_norm"
  comparison = lessthan        # greaterthan | lessthan | greaterorequal | lessorequal
  threshold  = 0.3
  iftrue     = 1.0
  iffalse    = 0.0
}
```

### `stringmatch`
```
consideration stringmatch {
  key      = "history.last_action_name"
  match    = "MeleeAttack"
  ifmatch   = 1.0
  ifnomatch = 0.0
}
```

### `composite`
```
consideration composite {
  allMustBeNonZero = true
  first targetexists { key = "target.player" }
  rest [
    multiply: curve { key = "float.dist_to_player_norm"  points = [[0,1],[1,0]] },
    multiply: threshold { key = "float.time_since_last_action_norm"  comparison = greaterthan  threshold = 0.5  iftrue = 1  iffalse = 0 }
  ]
}
```
`allMustBeNonZero = true` → short-circuit AND.
Operations: `multiply`, `average`, `add`, `min`, `max` (and abbreviations `mul`, `avg`, `*`, `+`).

---

## Extending the System

### Adding a new context key
1. Add a new `static readonly ContextKey<T>` field to any `[AIContextKey]`-tagged class.
2. Use its `InternalId` string in `.aidef` files — no parser changes needed.

### Adding a new consideration type
1. Create a new `[Serializable]` class inheriting `Consideration` in `ConsiderationBases`.
2. Add a `case` to `ConsiderationBuilder.Build` in `CodeGen.cs`.
3. Add a matching `ParseXxx(int line)` method and case in `AiDefParser.ParseConsideration`.
4. Add the matching serialise case in `AiDefCodeGenerator.SerialiseConsideration`.

---

## Adding a New Action Generator

1. Create a class implementing `IActionCodeGen`.
   - `ActionTypeKeyword` → the keyword that appears after `:` in the action block.
   - `Generate` → create a `ScriptableObject.CreateInstance<YourActionType>()`, populate it from the AST node, return a `GeneratedAction`.
   - `Serialise` → convert an existing asset back to text.
2. Register it: `generator.RegisterActionGen(new YourActionCodeGen())`.
   For enemy actions, do this in `EnemyAiCodeGeneratorFactory.Create()`.

Existing `.aidef` files that do not use the new keyword are unaffected.


