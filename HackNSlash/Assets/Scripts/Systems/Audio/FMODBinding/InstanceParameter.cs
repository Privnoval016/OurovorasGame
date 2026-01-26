using UnityEngine;
using UnityEngine.Serialization;

/**
 * <summary>
 * Component that holds a reference to an AudioParamValueSO to be used as an instance parameter for FMOD events.
 * </summary>
 */
public class InstanceParameter : MonoBehaviour
{
    [FormerlySerializedAs("surfaceParam")] public AudioParamValueSO instanceParam;
}