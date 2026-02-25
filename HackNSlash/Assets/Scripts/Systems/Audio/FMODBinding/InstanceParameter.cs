using UnityEngine;
using UnityEngine.Serialization;

/**
 * <summary>
 * Component that holds a reference to an AudioParamValueSO to be used as an instance parameter for FMOD events.
 * </summary>
 */
public class InstanceParameter : MonoBehaviour
{
    [Header("Main Instance Parameter")]
    public AudioParamValueSO instanceParam;
}