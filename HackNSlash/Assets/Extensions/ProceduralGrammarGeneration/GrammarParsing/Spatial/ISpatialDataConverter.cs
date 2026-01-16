using System;

namespace ProceduralGrammarGeneration.GrammarParsing
{
    /// <summary>
    /// Interface for converting Unity-specific spatial data to grammar-native types.
    /// This abstraction keeps the grammar backend Unity-independent while allowing
    /// seamless Unity Editor integration.
    /// </summary>
    public interface ISpatialDataConverter
    {
        /// <summary>
        /// Converts Unity spline data to grammar-native SplineCurve.
        /// </summary>
        /// <param name="unitySpline">Unity spline object</param>
        /// <returns>Grammar-native SplineCurve</returns>
        SplineCurve ConvertSpline(object unitySpline);

        /// <summary>
        /// Converts Unity terrain or texture data to grammar-native Heightmap.
        /// </summary>
        /// <param name="unityHeightData">Unity height data (Terrain, Texture2D, etc.)</param>
        /// <param name="width">Heightmap width</param>
        /// <param name="height">Heightmap height</param>
        /// <param name="worldSize">World-space size</param>
        /// <returns>Grammar-native Heightmap</returns>
        Heightmap ConvertHeightmap(object unityHeightData, int width, int height, Vector3D worldSize);

        /// <summary>
        /// Converts Unity Transform array or ParticleSystem to grammar-native PointField.
        /// </summary>
        /// <param name="unityPointData">Unity point data</param>
        /// <returns>Grammar-native PointField</returns>
        PointField ConvertPointField(object unityPointData);

        /// <summary>
        /// Gets the converter name (e.g., "Unity2022", "Unity2023").
        /// </summary>
        string ConverterName { get; }
    }
}
