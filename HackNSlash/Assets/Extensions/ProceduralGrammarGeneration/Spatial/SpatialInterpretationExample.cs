using UnityEngine;
using ProceduralGrammarGeneration.GrammarParsing;
using ProceduralGrammarGeneration.Runtime;

namespace ProceduralGrammarGeneration.Spatial
{
    /// <summary>
    /// Example demonstrating how to use the spatial interpretation system.
    /// Shows the complete pipeline: Grammar → Derivation Tree → Spatial Graph → Unity Scene
    /// </summary>
    public class SpatialInterpretationExample : MonoBehaviour
    {
        [Header("Grammar")]
        [Tooltip("Grammar asset to generate from")]
        public GrammarAsset grammarAsset;
        
        [Header("Geometry Library")]
        [Tooltip("Geometry data for terminal symbols (Window, Door, Wall, etc.)")]
        public SymbolGeometryData[] geometryLibrary;
        
        [Header("Spatial Settings")]
        [Tooltip("Height of each floor in meters")]
        public float heightPerFloor = 25f;
        
        [Tooltip("Create hierarchical GameObject structure (parent-child relationships)")]
        public bool hierarchical = true;
        
        [Header("Generation Parameters")]
        public int width = 100;
        public int height = 100;
        public int floors = 4;
        
        [ContextMenu("Generate Building")]
        public void GenerateBuilding()
        {
            if (grammarAsset == null)
            {
                Debug.LogError("Grammar asset not assigned!");
                return;
            }
            
            // Step 1: Compile grammar
            var engine = new GrammarEngine();
            var pgrContent = ExportGrammarToPGR(grammarAsset);
            var grammarDef = engine.CompileFromSource(pgrContent, out string error);
            
            if (grammarDef == null)
            {
                Debug.LogError($"Failed to compile grammar: {error}");
                return;
            }
            
            Debug.Log("✓ Grammar compiled");
            
            // Step 2: Generate derivation tree
            var parameters = new System.Collections.Generic.Dictionary<string, object>
            {
                { "width", (float)width },
                { "height", (float)height },
                { "floors", floors }
            };
            
            var derivationTree = engine.GenerateFromSymbol(grammarAsset.axiom, parameters, grammarAsset.maxIterations);
            
            if (derivationTree == null)
            {
                Debug.LogError("Failed to generate derivation tree");
                return;
            }
            
            Debug.Log($"✓ Derivation tree generated: {derivationTree.GetTotalNodeCount()} nodes");
            
            // Step 3: Create spatial interpreter with strategy
            var strategy = new VerticalStackStrategy(heightPerFloor);
            var interpreter = new SpatialInterpreter(strategy);
            
            // Register geometry library
            if (geometryLibrary != null && geometryLibrary.Length > 0)
            {
                interpreter.RegisterGeometryLibrary(geometryLibrary);
                Debug.Log($"✓ Registered {geometryLibrary.Length} geometry data assets");
            }
            else
            {
                Debug.LogWarning("No geometry library assigned - objects will have default appearance");
            }
            
            // Step 4: Interpret tree into spatial graph
            var spatialGraph = interpreter.Interpret(derivationTree);
            
            Debug.Log($"✓ Spatial graph created: {spatialGraph.NodesById.Count} nodes");
            var stats = spatialGraph.GetStats();
            Debug.Log($"  Terminal nodes: {stats.TerminalNodes}");
            Debug.Log($"  Max depth: {stats.MaxDepth}");
            Debug.Log($"  Bounds: {spatialGraph.Bounds}");
            
            // Step 5: Instantiate in Unity scene
            var instantiator = new UnitySceneInstantiator(transform, hierarchical);
            var buildingObject = instantiator.Instantiate(spatialGraph);
            
            Debug.Log($"✓ Building instantiated: {buildingObject.name}");
        }
        
        private string ExportGrammarToPGR(GrammarAsset asset)
        {
            // Simple export - in production use GrammarAssetIO.ExportToPGR
            var sb = new System.Text.StringBuilder();
            
            // Symbols
            foreach (var symbol in asset.symbols)
            {
                sb.Append($"symbol {symbol.name}");
                if (symbol.parameters.Count > 0)
                {
                    sb.Append("(");
                    for (int i = 0; i < symbol.parameters.Count; i++)
                    {
                        var p = symbol.parameters[i];
                        sb.Append($"{p.name}:{p.type.ToString().ToLower()}");
                        if (i < symbol.parameters.Count - 1)
                            sb.Append(", ");
                    }
                    sb.Append(")");
                }
                sb.AppendLine(";");
            }
            
            // Rules
            foreach (var rule in asset.rules)
            {
                sb.AppendLine($"rule {rule.name}");
                sb.Append($"    when {rule.predecessor.name}");
                if (rule.predecessor.parameters.Count > 0)
                {
                    sb.Append("(");
                    sb.Append(string.Join(", ", System.Array.ConvertAll(
                        rule.predecessor.parameters.ToArray(), p => p.name)));
                    sb.Append(")");
                }
                sb.AppendLine();
                
                foreach (var prod in rule.productions)
                {
                    if (prod.conditions.Count > 0)
                    {
                        sb.Append("    if (");
                        // Add conditions...
                        sb.AppendLine(")");
                    }
                    
                    sb.Append("    => ");
                    // Add production steps...
                    sb.AppendLine(";");
                }
            }
            
            return sb.ToString();
        }
    }
}
