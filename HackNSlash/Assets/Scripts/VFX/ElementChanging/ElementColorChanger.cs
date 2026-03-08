using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/** <summary>
 * Drives all <see cref="IElementChangeable"/> instances on an entity,
 * forwarding element-change events to each one.
 * </summary>
 */
public class ElementColorChanger : MonoBehaviour
{
    public List<ElementUpdateInfo> elementUpdateInfos;

    private Func<ElementEffect> getCurrentElementEffect;
    private ElementEffect currentElement;

    /** <summary>Initialises all changeables with the provided element getter.</summary> */
    public void Initialize(Func<ElementEffect> elementFunc)
    {
        getCurrentElementEffect = elementFunc;

        foreach (var info in elementUpdateInfos)
            info.elementChangeable.Initialize();

        currentElement = elementFunc();
        UpdateAllDependents();
    }

    private void Update()
    {
        if (getCurrentElementEffect == null) return;
        if (getCurrentElementEffect() == currentElement) return;

        currentElement = getCurrentElementEffect();
        UpdateAllDependents();
    }

    private void UpdateAllDependents()
    {
        foreach (var info in elementUpdateInfos)
            info.elementChangeable.UpdateElement(currentElement);
    }
}

[Serializable]
public struct ElementUpdateInfo
{
    [SerializeReference] public IElementChangeable elementChangeable;
}