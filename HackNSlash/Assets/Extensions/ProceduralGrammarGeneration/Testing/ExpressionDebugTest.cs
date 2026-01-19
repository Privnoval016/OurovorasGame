using UnityEngine;
using ProceduralGrammarGeneration.GrammarParsing;
using System.Collections.Generic;

namespace ProceduralGrammarGeneration.Testing
{
    /// <summary>
    /// Debug test to verify expression evaluation with parentheses
    /// </summary>
    public class ExpressionDebugTest : MonoBehaviour
    {
        [ContextMenu("Test Expression Evaluation")]
        public void TestExpressionEvaluation()
        {
            string testGrammar = @"
// Test Grammar - Expression Evaluation
symbol Test(a:float, b:float, result:float);
symbol terminal Final(value:float);

rule TestRule
    when Test(a, b, result)
    => Final(value = a + b);
";

            var engine = new GrammarEngine();
            if (!engine.CompileGrammar(testGrammar, out string error))
            {
                Debug.LogError($"Compilation failed: {error}");
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                { "a", 1.5f },
                { "b", 2.0f },
                { "result", 0f }
            };

            var tree = engine.GenerateFromSymbol("Test", parameters, maxIterations: 10);
            
            Debug.Log("=== Expression Evaluation Test ===");
            Debug.Log($"Input: a=1.5, b=2.0");
            Debug.Log($"Expected: a + b = 3.5");
            
            PrintTree(tree.Root, 0);
        }

        [ContextMenu("Test Division With Parentheses")]
        public void TestDivisionWithParentheses()
        {
            string testGrammar = @"
// Test Grammar - Division with Parentheses
symbol Test(length:float, doorWidth:float, wallWidth:float);
symbol Result(count1:int, count2:int);
symbol terminal Final;

rule TestRule
    when Test(length, doorWidth, wallWidth)
    => Result(count1 = length / (doorWidth + wallWidth), count2 = length / 3.5) Final;
";

            var engine = new GrammarEngine();
            if (!engine.CompileGrammar(testGrammar, out string error))
            {
                Debug.LogError($"Compilation failed: {error}");
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                { "length", 40f },
                { "doorWidth", 1.5f },
                { "wallWidth", 2.0f }
            };

            var tree = engine.GenerateFromSymbol("Test", parameters, maxIterations: 10);
            
            Debug.Log("=== Division With Parentheses Test ===");
            Debug.Log($"Input: length=40, doorWidth=1.5, wallWidth=2.0");
            Debug.Log($"Expected: length / (doorWidth + wallWidth) = 40 / 3.5 = 11.428");
            Debug.Log($"Expected: length / 3.5 = 40 / 3.5 = 11.428");
            
            PrintTree(tree.Root, 0);
        }

        private void PrintTree(DerivationNode node, int depth)
        {
            string indent = new string(' ', depth * 2);
            string paramStr = "";
            
            if (node.Symbol.Parameters.Count > 0)
            {
                var pairs = new List<string>();
                foreach (var kvp in node.Symbol.Parameters)
                {
                    pairs.Add($"{kvp.Key}={kvp.Value}");
                }
                paramStr = $" ({string.Join(", ", pairs)})";
            }
            
            Debug.Log($"{indent}- {node.Symbol.Type.Name}{paramStr}");
            
            foreach (var child in node.Children)
            {
                PrintTree(child, depth + 1);
            }
        }
    }
}
