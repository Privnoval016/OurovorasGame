# File I/O Architecture

## Overview

The grammar system uses an interface-based architecture for file I/O, making it easy to swap between different file loading strategies (file system, Unity Resources, Unity Addressables, etc.).

## Architecture

### Interface: `IGrammarFileReader`

Located in: `IGrammarFileReader.cs`

```csharp
public interface IGrammarFileReader
{
    bool Exists(string filePath);
    string ReadAllText(string filePath);
    string ReaderName { get; }
}
```

### Implementations

#### 1. **FileSystemGrammarReader** (Default)
- **File**: `FileSystemGrammarReader.cs`
- **Use Case**: Development, testing, standalone builds
- **Features**:
  - Reads .pgr files directly from file system
  - Supports absolute and relative paths
  - Optional base path for resolving relative paths

**Usage**:
```csharp
// Use default (current directory)
var engine = new GrammarEngine();

// Or specify base path
var reader = new FileSystemGrammarReader("/path/to/grammars");
var engine = new GrammarEngine(reader);
```

#### 2. **UnityResourcesGrammarReader** (Unity)
- **File**: `UnityResourcesGrammarReader.cs`
- **Use Case**: Unity runtime with Resources folder
- **Features**:
  - Loads .pgr files from Unity's Resources folder
  - Place grammar files in `Assets/Resources/Grammars/`
  - Automatically strips .pgr extension for Resources.Load

**Usage**:
```csharp
// In Unity
var reader = new UnityResourcesGrammarReader("Grammars");
var engine = new GrammarEngine(reader);
engine.CompileGrammarFromFile("SimpleBuilding.pgr", out string error);
```

## Switching Between Implementations

The `GrammarEngine` can switch file readers at runtime:

```csharp
var engine = new GrammarEngine();

// Start with file system
engine.CompileGrammarFromFile("dev_grammar.pgr", out _);

// Switch to Unity Resources
engine.SetFileReader(new UnityResourcesGrammarReader());
engine.CompileGrammarFromFile("production_grammar.pgr", out _);
```

## Comment Support

Grammar files (.pgr) support both single-line and multi-line comments:

```
// Single-line comment

/* 
 * Multi-line comment
 * Can span multiple lines
 */

symbol Building(width: float, height: float);  // Inline comment
```

Comments are automatically stripped during lexical analysis.

## Creating Custom Readers

To create a custom file reader (e.g., for Unity Addressables, web resources, etc.):

1. Implement `IGrammarFileReader`
2. Implement `Exists()` to check file availability
3. Implement `ReadAllText()` to load file contents
4. Provide a descriptive `ReaderName`

Example:
```csharp
public class WebGrammarReader : IGrammarFileReader
{
    public string ReaderName => "Web";
    
    public bool Exists(string filePath)
    {
        // Check if URL is accessible
        return true;
    }
    
    public string ReadAllText(string filePath)
    {
        // Download and return content
        using var client = new HttpClient();
        return client.GetStringAsync(filePath).Result;
    }
}
```

## Benefits

1. **Testability**: Easy to mock for unit tests
2. **Flexibility**: Switch sources without changing grammar engine
3. **Unity Integration**: Seamless transition from development to Unity
4. **Future-Proof**: Easy to add new sources (Addressables, streaming assets, etc.)

## Migration Path

### Phase 1: Current (File System)
```csharp
var engine = new GrammarEngine();  // Uses FileSystemGrammarReader by default
```

### Phase 2: Unity Editor
```csharp
#if UNITY_EDITOR
var engine = new GrammarEngine(new FileSystemGrammarReader(Application.dataPath + "/Grammars"));
#else
var engine = new GrammarEngine(new UnityResourcesGrammarReader());
#endif
```

### Phase 3: Unity UI
- Create custom Unity Editor window
- Use `UnityResourcesGrammarReader` for runtime
- Grammar files managed as Unity assets
- UI provides grammar selection, editing, and real-time preview
