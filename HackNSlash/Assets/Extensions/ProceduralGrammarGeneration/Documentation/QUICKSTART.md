# Quick Start Guide

## Getting Started in 5 Minutes

### 1. Basic Usage

```csharp
using ProceduralGrammarGeneration.GrammarParsing;
using System.Collections.Generic;

// Create engine
var engine = new GrammarEngine();

// Define grammar
var grammar = @"
    symbol Building(width: float, height: float);
    symbol terminal Floor(width: float);
    
    rule SimpleBuilding
        when Building(width, height)
        => repeat(Y, 3) {
            Floor(width = width)
        };
";

// Compile
if (engine.CompileGrammar(grammar, out string error))
{
    // Generate
    var startSymbol = new Symbol(
        new SymbolType("Building", 0),
        new Dictionary<string, object> {
            { "width", 10f },
            { "height", 15f }
        }
    );
    
    var tree = engine.Generate(startSymbol, seed: 42);
    
    // Use the tree
    foreach (var node in tree.GetTerminalNodes())
    {
        UnityEngine.Debug.Log($"Terminal: {node.Symbol.Type.Name}");
    }
}
else
{
    UnityEngine.Debug.LogError(error);
}
```

### 2. Grammar Syntax Cheat Sheet

```
// Symbol declaration
symbol SymbolName(param: type);
symbol terminal TerminalName(param: type);

// Rule
rule RuleName
    when SymbolName(param)
    if condition
    => expansion;

// Expansions
Production: Symbol1(arg = value), Symbol2(arg = value)
Split:      split(Y) { size1: expansion1, size2: expansion2 }
Repeat:     repeat(X, count) { expansion }
Choose:     choose { default: exp1, weight 0.5: exp2 }

// Conditions
param == value
param > value && param < otherValue
!(param == value)

// Expressions
width * 0.5
height / count
(width - 10) * 2
```

### 3. Common Patterns

**Building with Floors:**
```
rule FacadeToFloors
    when Facade(width, height, floors)
    => repeat(Y, floors) {
        Floor(width = width, height = height / floors, index = 0)
    };
```

**Conditional Rules:**
```
rule GroundFloor
    when Floor(width, height, index)
    if index == 0
    => Door(width = width * 0.9);

rule UpperFloor
    when Floor(width, height, index)
    if index > 0
    => Window(width = width * 0.7);
```

**Choice with Weights:**
```
rule RandomFeature
    when Bay(width, height)
    => choose {
        default: Window(width = width),
        weight 0.3: Door(width = width),
        weight 0.1: Empty()
    };
```

**Split Operation:**
```
rule DivideFacade
    when Facade(width, height)
    => split(X) {
        5.0: Column(width = 5.0, height = height),
        width - 10.0: Main(width = width - 10.0, height = height),
        5.0: Column(width = 5.0, height = height)
    };
```

### 4. Working with Overrides

```csharp
// Get override manager
var overrides = engine.GetOverrides();

// Force specific rule at a scope
overrides.SetRuleOverride(
    new ScopeId("Root/Floor[0]"), 
    ruleId: 5
);

// Force specific choice option
overrides.SetChoiceOverride(
    new ScopeId("Root/Floor[1]/Bay[0]"),
    ruleId: 3,
    choiceIndex: 1  // Select second option
);

// Clear overrides
overrides.ClearAll();

// Regenerate with overrides
var tree = engine.Generate(startSymbol, seed: 42);
```

### 5. Error Handling

```csharp
var pipeline = new GrammarCompilerPipeline(source);
var result = pipeline.Compile();

if (!result.Success)
{
    // Show errors
    foreach (var error in result.Errors)
        UnityEngine.Debug.LogError(error.Message);
    
    // Show warnings
    foreach (var warning in result.Warnings)
        UnityEngine.Debug.LogWarning(warning.Message);
}
```

### 6. Working with Derivation Trees

```csharp
var tree = engine.Generate(startSymbol, seed: 42);

// Get statistics
int totalNodes = tree.GetTotalNodeCount();
int maxDepth = tree.GetMaxDepth();

// Iterate all nodes
foreach (var node in tree.GetAllNodes())
{
    UnityEngine.Debug.Log($"{node.Symbol.Type.Name} at depth {node.Depth}");
}

// Get terminal nodes only
foreach (var terminal in tree.GetTerminalNodes())
{
    // These are the "leaf" symbols ready for geometry binding
    var width = terminal.Symbol.GetParameter<float>("width");
    var height = terminal.Symbol.GetParameter<float>("height");
}

// Find specific node by scope
var node = tree.GetNode(new ScopeId("Root/Floor[0]/Bay[1]"));
if (node != null)
{
    UnityEngine.Debug.Log($"Found: {node.Symbol.Type.Name}");
}
```

### 7. Deterministic Generation

```csharp
// Same seed always produces same result
var tree1 = engine.Generate(startSymbol, seed: 12345);
var tree2 = engine.Generate(startSymbol, seed: 12345);
// tree1 and tree2 are identical

// Different seeds produce variation
var tree3 = engine.Generate(startSymbol, seed: 67890);
// tree3 is different from tree1/tree2
```

### 8. Running Tests

```csharp
using ProceduralGrammarGeneration.GrammarParsing;

// Run all tests
GrammarSystemTest.RunAllTests();

// Run complete workflow demo
GrammarSystemTest.DemoCompleteWorkflow();
```

## Example Grammars

### Simple Building
```
symbol Building(width: float, height: float);
symbol Floor(width: float, height: float);
symbol terminal Room(width: float, height: float);

rule BuildingToFloors
    when Building(width, height)
    => repeat(Y, 5) {
        Floor(width = width, height = height / 5)
    };

rule FloorToRoom
    when Floor(width, height)
    => Room(width = width, height = height);
```

### Building with Windows and Doors
```
symbol Facade(width: float, height: float, floors: int);
symbol Floor(width: float, height: float, index: int);
symbol Bay(width: float, height: float, index: int);
symbol terminal Window(width: float, height: float);
symbol terminal Door(width: float, height: float);

rule FacadeToFloors
    when Facade(width, height, floors)
    => repeat(Y, floors) {
        Floor(width = width, height = height / floors, index = 0)
    };

rule FloorToBays
    when Floor(width, height, index)
    => repeat(X, 4) {
        Bay(width = width / 4, height = height, index = index)
    };

rule GroundBay
    when Bay(width, height, index)
    if index == 0
    => choose {
        default: Window(width = width * 0.8, height = height * 0.6),
        weight 0.3: Door(width = width * 0.9, height = height * 0.9)
    };

rule UpperBay
    when Bay(width, height, index)
    if index > 0
    => Window(width = width * 0.7, height = height * 0.5);
```

See `ExampleGrammar.txt` for more complete examples.

## Common Issues

**Q: "Undefined symbol 'X'"**
- Make sure all symbols are declared before they're used in rules
- Check for typos in symbol names

**Q: "Rule conflict detected"**
- Multiple rules match the same symbol without conditions
- Add conditions to disambiguate, or use rule priorities

**Q: "Potentially infinite recursion"**
- Grammar has cycles without termination conditions
- Add conditionals or terminal symbols to break cycles

**Q: "Parameter not found"**
- Using a parameter that wasn't defined in the symbol/rule
- Check parameter names match exactly (case-sensitive)

## Next Steps

1. Read the full [README.md](README.md) for detailed documentation
2. Check [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md) for architecture details
3. Study [ExampleGrammar.txt](ExampleGrammar.txt) for complete examples
4. Run `GrammarSystemTest` to see the system in action

## Support

For issues or questions:
1. Check the README.md documentation
2. Review the example grammar
3. Run the test suite to validate your setup

Happy generating! 🏗️
