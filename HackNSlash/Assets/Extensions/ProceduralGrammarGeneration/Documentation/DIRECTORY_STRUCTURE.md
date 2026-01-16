# Directory Structure

This document describes the organization of the Procedural Grammar Generation system for Unity.

## Folder Organization

```
ProceduralGrammarGeneration/
├── Documentation/              # All documentation files
│   ├── README.md
│   ├── QUICKSTART.md
│   ├── FILE_IO_ARCHITECTURE.md
│   ├── SPATIAL_SYSTEM.md
│   ├── SPATIAL_QUICKSTART.md
│   ├── EDITOR_QUICKSTART.md
│   ├── IMPLEMENTATION_SUMMARY.md
│   ├── INDEX.md
│   └── DIRECTORY_STRUCTURE.md (this file)
│
├── Editor/                     # Unity Editor integration (requires UnityEngine)
│   ├── GrammarAsset.cs                # ScriptableObject for storing grammars
│   ├── GrammarAssetIO.cs              # Bridge between Unity assets and .pgr files
│   ├── SerializedGrammarTypes.cs      # Unity-serializable grammar data structures
│   ├── UnitySpatialConverter.cs       # Unity spatial data converter
│   ├── GrammarEditorWindow.cs         # Visual grammar editor window
│   ├── GrammarAssetEditor.cs          # Custom inspector for GrammarAsset
│   └── GrammarMenuItems.cs            # Menu items and shortcuts
│
└── GrammarParsing/             # Backend system (Unity-independent)
    ├── Core/                   # Core grammar type definitions
    │   ├── GrammarTypes.cs        # Symbol, ParameterType, SymbolType definitions
    │   ├── GrammarDefinition.cs   # Grammar rule definitions
    │   └── OverrideSystem.cs      # Parameter override system
    │
    ├── Parsing/                # Lexical analysis and parsing
    │   ├── GrammarLexer.cs        # Tokenization
    │   ├── GrammarParser.cs       # Syntax analysis
    │   └── SemanticAnalyzer.cs    # Semantic validation
    │
    ├── Execution/              # Grammar compilation and execution
    │   ├── GrammarIR.cs           # Intermediate representation
    │   ├── GrammarEngine.cs       # Main engine and compiler
    │   └── DerivationTree.cs      # Tree structure for derivations
    │
    ├── FileIO/                 # File input/output abstraction
    │   ├── IGrammarFileReader.cs           # Interface for reading grammar files
    │   ├── FileSystemGrammarReader.cs      # Standard file system implementation
    │   └── UnityResourcesGrammarReader.cs  # Unity Resources folder implementation
    │
    ├── Spatial/                # Spatial data structures
    │   ├── SpatialTypes.cs            # Vector3D, SplineCurve, Heightmap, PointField
    │   └── ISpatialDataConverter.cs   # Interface for Unity conversion
    │
    ├── SimpleBuilding.pgr      # Example grammar file (simple)
    ├── ComplexBuilding.pgr     # Example grammar file (complex)
    └── ExampleGrammar.txt      # Example grammar file (text format)

```

## Architecture Separation

### Backend (GrammarParsing/)
**Unity-independent code** - Can run in standalone .NET environments or be ported to other engines.

- **No UnityEngine dependencies**
- Pure C# logic for grammar parsing and execution
- Spatial math using custom Vector3D (not UnityEngine.Vector3)
- Can be tested in standalone .NET applications

### Unity Integration (Editor/)
**Unity-dependent code** - Requires UnityEngine and UnityEditor APIs.

- **Uses UnityEngine types** (ScriptableObject, GameObject, Terrain, etc.)
- Visual editor tools for designers
- Asset management and serialization
- Conversion layer between Unity and backend types

## File Formats

- **`.pgr`** - Procedural Grammar Rule files (text format)
- **`.cs`** - C# source files
- **`.asset`** - Unity ScriptableObject assets (GrammarAsset)
- **`.md`** - Markdown documentation

## Key Components

### GrammarParsing (Backend)

#### Core
Contains fundamental types that define what a grammar is: symbols, parameters, rules.

#### Parsing
Transforms text `.pgr` files into structured data through lexical analysis, parsing, and semantic validation.

#### Execution  
Compiles validated grammars into executable form and manages the derivation process.

#### FileIO
Provides abstraction for reading grammar files from different sources (file system, Unity Resources, etc.).

#### Spatial
Unity-independent spatial data structures (splines, heightmaps, point fields) with converter interface.

### Editor (Unity Integration)

#### Asset Management
- **GrammarAsset**: ScriptableObject for storing grammar definitions
- **SerializedGrammarTypes**: Unity-serializable wrappers for grammar data

#### Visual Editing
- **GrammarEditorWindow**: Full-featured visual editor with 4-tab interface
- **GrammarAssetEditor**: Custom inspector with quick actions
- **GrammarMenuItems**: Menu shortcuts and context menu items

#### Conversion Layer
- **GrammarAssetIO**: Bridges between Unity assets and backend grammar system
- **UnitySpatialConverter**: Converts Unity spatial data to backend types

## Usage in Unity

1. Place your `.pgr` grammar files in the GrammarParsing folder or Resources folder
2. Or create **GrammarAsset** ScriptableObjects using the visual editor
3. Use `GrammarAssetIO.CompileGrammar(asset)` to compile from assets
4. Use `GrammarEngine` directly to load .pgr files
5. Use `UnitySpatialConverter` to convert Unity spatial data
6. Execute grammars to generate procedural content
7. Process the resulting `DerivationTree` to create geometry

## Namespaces

- **Backend**: `ProceduralGrammarGeneration.GrammarParsing`
- **Unity Integration**: `ProceduralGrammarGeneration.Editor`

## Example Usage

```csharp
using UnityEngine;
using ProceduralGrammarGeneration.GrammarParsing;
using ProceduralGrammarGeneration.Editor;

public class ProceduralGenerator : MonoBehaviour
{
    public GrammarAsset grammarAsset;
    
    void Start()
    {
        // Option 1: Use visual grammar asset
        var engine = GrammarAssetIO.CompileGrammar(grammarAsset);
        
        // Option 2: Load from .pgr file (backend only)
        var engine2 = new GrammarEngine();
        engine2.CompileGrammarFromFile("path/to/grammar.pgr");
        
        // Execute
        var result = engine.Execute(grammarAsset.axiom, grammarAsset.maxIterations);
        
        Debug.Log($"Generated {result.NodeCount} nodes");
    }
}
```
