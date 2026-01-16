using System;

namespace ProceduralGrammarGeneration.GrammarParsing
{
    /// <summary>
    /// Interface for reading grammar files from various sources.
    /// This abstraction allows swapping between file system, Unity resources, or other sources.
    /// </summary>
    public interface IGrammarFileReader
    {
        /// <summary>
        /// Checks if a grammar file exists at the specified path.
        /// </summary>
        /// <param name="filePath">The path to the grammar file</param>
        /// <returns>True if the file exists, false otherwise</returns>
        bool Exists(string filePath);

        /// <summary>
        /// Reads the entire contents of a grammar file.
        /// </summary>
        /// <param name="filePath">The path to the grammar file</param>
        /// <returns>The file contents as a string</returns>
        /// <exception cref="System.IO.FileNotFoundException">Thrown when the file does not exist</exception>
        /// <exception cref="System.IO.IOException">Thrown when an I/O error occurs</exception>
        string ReadAllText(string filePath);

        /// <summary>
        /// Gets a display name for the file reader (e.g., "FileSystem", "UnityResources").
        /// </summary>
        string ReaderName { get; }
    }
}
