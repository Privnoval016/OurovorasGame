using Extensions.Data;
using UnityEngine;

/**
 * <summary>
 * Lookup table mapping surface materials to audio parameter values.
 * </summary>
 */
[CreateAssetMenu(menuName = "AudioReferences/Surface Lookup Table")]
public class SurfaceLookupTable : LookupTable<Material, AudioParamValueSO> { }