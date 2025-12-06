using System;
using UnityEngine;

/**
 * <summary>
 * ScriptableObject wrapper for AudioParamValue struct.
 * </summary>
 */
[CreateAssetMenu(menuName = "AudioReferences/Param Value")]
public class AudioParamValueSO : ScriptableObject
{
    public AudioParamValue param;
}

/**
 * <summary>
 * Wrapper struct for an audio parameter and its associated value.
 * </summary>
 */
[Serializable]
public struct AudioParamValue
{
    public AudioParameter parameter;
    public float value;

    public AudioParamValue(AudioParameter param, float val)
    {
        parameter = param;
        value = val;
    }
    
    /**
     * <summary>
     * Replaces an existing parameter in the array with a new parameter value.
     * </summary>
     *
     * <param name="originalParams">The original array of AudioParamValue.</param>
     * <param name="newParam">The new AudioParamValue to insert.</param>
     *
     * <returns>The updated array of AudioParamValue.</returns>
     */
    public static AudioParamValue[] ReplaceParameter(AudioParamValue[] originalParams, AudioParamValue newParam)
    {
        for (int i = 0; i < originalParams.Length; i++)
        {
            if (originalParams[i].parameter == newParam.parameter)
            {
                originalParams[i] = newParam;
                return originalParams;
            }
        }

        // If the parameter was not found, return the original array unchanged.
        return originalParams;
    }
}