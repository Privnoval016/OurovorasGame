using System;

namespace ProceduralGrammarGeneration.GrammarParsing
{
    /// <summary>
    /// Example Unity Resources implementation of IGrammarFileReader.
    /// This shows how to load grammar files from Unity's Resources folder.
    /// Usage in Unity: Place .pgr files in Assets/Resources/Grammars/
    /// </summary>
    public class UnityResourcesGrammarReader : IGrammarFileReader
    {
        private readonly string _resourcesFolder;

        /// <summary>
        /// Creates a new UnityResourcesGrammarReader.
        /// </summary>
        /// <param name="resourcesFolder">Path within Resources folder (e.g., "Grammars")</param>
        public UnityResourcesGrammarReader(string resourcesFolder = "Grammars")
        {
            _resourcesFolder = resourcesFolder;
        }

        public string ReaderName => "UnityResources";

        public bool Exists(string filePath)
        {
            // In Unity, this would use Resources.Load<TextAsset>
            // For now, this is a placeholder showing the interface
            var resourcePath = GetResourcePath(filePath);
            
            #if UNITY_EDITOR || UNITY_STANDALONE
            var asset = UnityEngine.Resources.Load<UnityEngine.TextAsset>(resourcePath);
            return asset != null;
            #else
            throw new NotSupportedException("UnityResourcesGrammarReader requires Unity runtime");
            #endif
        }

        public string ReadAllText(string filePath)
        {
            var resourcePath = GetResourcePath(filePath);
            
            #if UNITY_EDITOR || UNITY_STANDALONE
            var asset = UnityEngine.Resources.Load<UnityEngine.TextAsset>(resourcePath);
            if (asset == null)
            {
                throw new System.IO.FileNotFoundException(
                    $"Grammar resource not found: {resourcePath}", resourcePath);
            }
            return asset.text;
            #else
            throw new NotSupportedException("UnityResourcesGrammarReader requires Unity runtime");
            #endif
        }

        private string GetResourcePath(string filePath)
        {
            // Remove .pgr extension as Resources.Load doesn't use extensions
            var pathWithoutExtension = filePath.EndsWith(".pgr") 
                ? filePath.Substring(0, filePath.Length - 4) 
                : filePath;
            
            // Combine with resources folder
            if (string.IsNullOrEmpty(_resourcesFolder))
                return pathWithoutExtension;
            
            return $"{_resourcesFolder}/{pathWithoutExtension}";
        }
    }
}
