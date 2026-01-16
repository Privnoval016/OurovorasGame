using System;
using System.IO;

namespace ProceduralGrammarGeneration.GrammarParsing
{
    /// <summary>
    /// File system implementation of IGrammarFileReader.
    /// Reads grammar files directly from the file system.
    /// </summary>
    public class FileSystemGrammarReader : IGrammarFileReader
    {
        private readonly string _basePath;

        /// <summary>
        /// Creates a new FileSystemGrammarReader with an optional base path.
        /// </summary>
        /// <param name="basePath">Optional base directory for relative paths. If null, uses current directory.</param>
        public FileSystemGrammarReader(string? basePath = null)
        {
            _basePath = basePath ?? Directory.GetCurrentDirectory();
        }

        public string ReaderName => "FileSystem";

        public bool Exists(string filePath)
        {
            var fullPath = GetFullPath(filePath);
            return File.Exists(fullPath);
        }

        public string ReadAllText(string filePath)
        {
            var fullPath = GetFullPath(filePath);
            
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Grammar file not found: {fullPath}", fullPath);
            }

            return File.ReadAllText(fullPath);
        }

        private string GetFullPath(string filePath)
        {
            // If already an absolute path, use it directly
            if (Path.IsPathRooted(filePath))
            {
                return filePath;
            }

            // Otherwise, combine with base path
            return Path.Combine(_basePath, filePath);
        }
    }
}
