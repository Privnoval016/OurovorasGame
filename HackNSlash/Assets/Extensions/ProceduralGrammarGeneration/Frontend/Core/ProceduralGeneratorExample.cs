using UnityEngine;
using ProceduralGrammarGeneration.Frontend;
using ProceduralGrammarGeneration.GrammarParsing;

namespace ProceduralGrammarGeneration.Examples
{
    /// <summary>
    /// Example script showing how to use ProceduralGenerator from code.
    /// Demonstrates runtime generation and parameter modification.
    /// </summary>
    public class ProceduralGeneratorExample : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Reference to the ProceduralGenerator component")]
        public ProceduralGenerator generator;
        
        [Header("Runtime Controls")]
        [Tooltip("Generate on start")]
        public bool generateOnStart = false;
        
        [Tooltip("Regenerate every N seconds (0 = disabled)")]
        public float autoRegenerateInterval = 0f;
        
        [Header("Parameter Animation")]
        [Tooltip("Animate a specific parameter")]
        public bool animateParameter = false;
        
        [Tooltip("Parameter name to animate")]
        public string animatedParameterName = "length";
        
        [Tooltip("Min/Max values for animation")]
        public Vector2 animationRange = new Vector2(20f, 60f);
        
        [Tooltip("Animation speed")]
        public float animationSpeed = 1f;
        
        private float _nextRegenerateTime;
        private float _animationTime;
        
        private void Start()
        {
            if (generator == null)
            {
                generator = GetComponent<ProceduralGenerator>();
            }
            
            if (generator == null)
            {
                Debug.LogError("ProceduralGeneratorExample: No ProceduralGenerator component found!");
                return;
            }
            
            if (generateOnStart)
            {
                generator.Generate();
            }
            
            _nextRegenerateTime = Time.time + autoRegenerateInterval;
        }
        
        private void Update()
        {
            if (generator == null) return;
            
            // Auto-regenerate
            if (autoRegenerateInterval > 0 && Time.time >= _nextRegenerateTime)
            {
                if (animateParameter)
                {
                    AnimateParameter();
                }
                
                generator.Generate();
                _nextRegenerateTime = Time.time + autoRegenerateInterval;
            }
            
            // Manual controls
            if (Input.GetKeyDown(KeyCode.G))
            {
                Debug.Log("ProceduralGeneratorExample: Generating (pressed G)");
                generator.Generate();
            }
            
            if (Input.GetKeyDown(KeyCode.C))
            {
                Debug.Log("ProceduralGeneratorExample: Clearing (pressed C)");
                generator.Clear();
            }
        }
        
        private void AnimateParameter()
        {
            _animationTime += animationSpeed * autoRegenerateInterval;
            float t = (Mathf.Sin(_animationTime) + 1f) / 2f; // Oscillate 0-1
            float value = Mathf.Lerp(animationRange.x, animationRange.y, t);
            
            // Find and update parameter
            var param = generator.axiomParameters.Find(p => p.name == animatedParameterName);
            if (param != null && param.type == ParameterType.Float)
            {
                param.floatValue = value;
                Debug.Log($"ProceduralGeneratorExample: Animated {animatedParameterName} = {value:F2}");
            }
        }
        
        /// <summary>
        /// Generate with custom parameters (callable from UI buttons, etc.)
        /// </summary>
        public void GenerateWithParameters(float param1, float param2, float param3)
        {
            if (generator == null) return;
            
            // Set parameters by index (assumes you know the order)
            if (generator.axiomParameters.Count >= 3)
            {
                if (generator.axiomParameters[0].type == ParameterType.Float)
                    generator.axiomParameters[0].floatValue = param1;
                    
                if (generator.axiomParameters[1].type == ParameterType.Float)
                    generator.axiomParameters[1].floatValue = param2;
                    
                if (generator.axiomParameters[2].type == ParameterType.Float)
                    generator.axiomParameters[2].floatValue = param3;
            }
            
            generator.Generate();
        }
        
        /// <summary>
        /// Example: Randomize all float parameters
        /// </summary>
        [ContextMenu("Randomize Parameters")]
        public void RandomizeParameters()
        {
            if (generator == null) return;
            
            foreach (var param in generator.axiomParameters)
            {
                if (param.type == ParameterType.Float)
                {
                    param.floatValue = Random.Range(10f, 50f);
                }
                else if (param.type == ParameterType.Int)
                {
                    param.intValue = Random.Range(5, 20);
                }
            }
            
            Debug.Log("ProceduralGeneratorExample: Randomized all parameters");
        }
        
        /// <summary>
        /// Example: Scale all float parameters by a factor
        /// </summary>
        public void ScaleParameters(float scaleFactor)
        {
            if (generator == null) return;
            
            foreach (var param in generator.axiomParameters)
            {
                if (param.type == ParameterType.Float)
                {
                    param.floatValue *= scaleFactor;
                }
            }
            
            Debug.Log($"ProceduralGeneratorExample: Scaled parameters by {scaleFactor}");
        }
    }
}
