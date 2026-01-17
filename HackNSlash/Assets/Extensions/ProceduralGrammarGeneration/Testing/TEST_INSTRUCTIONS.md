# Grammar System Test Instructions

## Setup

1. **Create a Test Scene**
   - Create a new empty GameObject in your Unity scene
   - Name it "GrammarTester"
   - Add the `GrammarTester` component to it

2. **Assign the Grammar Asset**
   - In the Inspector, find the "Grammar Configuration" section
   - Assign a GrammarAsset to the `Grammar Asset` field
   - Try using either:
     - `SimpleBuilding.pgr` (if imported as asset)
     - `ComplexBuilding.pgr` (if imported as asset)
     - Or any custom GrammarAsset you've created

3. **Configure Parameters**
   - The `Axiom Parameters` list will automatically populate based on your selected grammar
   - Adjust the parameter values as needed (e.g., width=10, height=20, floors=3)
   - Set the number of iterations (default: 5)
   - Set an output filename (default: `grammar_output.txt`)

## Running the Test

### Option 1: Context Menu
1. Right-click on the `GrammarTester` component in the Inspector
2. Select "Test Grammar Generation"

### Option 2: Code Button
You can also call `TestGrammarGeneration()` from a custom editor button or another script.

## What Gets Tested

The test validates:
1. **Grammar Compilation** - Converts your GrammarAsset to .pgr text and compiles it
2. **Parameter Handling** - Uses the axiom parameters you specified
3. **Derivation** - Runs the grammar engine for the specified number of iterations
4. **Output Generation** - Creates a detailed text report of the derivation tree

## Output

The test will:
- Print detailed logs to the Unity Console
- Display the output in the `Last Generated Output` field (Inspector)
- Write a text file to `Assets/Extensions/ProceduralGrammarGeneration/Testing/{outputFilename}`

## Expected Output Format

```
=== Grammar Derivation Result ===
Grammar: ComplexBuilding
Axiom: Facade
Iterations: 5
Total Steps: X

=== Derivation Steps ===

Step 1:
  Symbol: Facade
  Parameters:
    width = 10
    height = 20
    floors = 3
  Rule Applied: FacadeToFloors (production 0)
  Children:
    - Floor (width=10, height=6.666667, floorNum=1)

Step 2:
  Symbol: Floor
  Parameters:
    width = 10
    height = 6.666667
    floorNum = 1
  Rule Applied: UpperFloorToWindows (production 0)
  Children:
    - Window (width=2, height=6.666667)

...

=== Terminal Symbols (Leaf Nodes) ===
Total: X

1. Wall [width=2, height=6.666667]
2. Wall [width=2, height=6.666667]
...
```

## Troubleshooting

### "No grammar asset assigned!"
- Make sure you've dragged a GrammarAsset into the Inspector field

### "Failed to compile grammar"
- Check that your grammar has proper `symbol` and `rule` keywords
- Verify all symbols are defined before being used in rules
- Check the Console for detailed parser errors

### "Axiom symbol not found"
- Make sure the axiom name matches a defined symbol exactly
- Check for typos in the Settings tab of your GrammarAsset

### "Derivation failed!"
- Ensure you have at least one rule that matches your axiom
- Check that your rules have valid conditions and productions
- Try reducing the number of iterations

## Example Test Case

**ComplexBuilding Grammar:**
```
Axiom Parameters:
- width: 10
- height: 20
- floors: 3

Iterations: 5
```

**Expected Result:**
- 1 Facade symbol expands to 1 Floor
- 1 Floor expands to Windows (if floorNum > 0) or Doors (if floorNum == 0)
- Windows/Doors expand to Walls
- Final output shows the complete derivation tree with all parameter calculations
