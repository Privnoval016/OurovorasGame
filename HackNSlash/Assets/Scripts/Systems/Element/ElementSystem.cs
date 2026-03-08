using System;
using System.Collections.Generic;
using UnityEngine;

public class ElementSystem : MonoBehaviour, IElementSystem
{
    public ElementData[] elementData;
    private Dictionary<ElementEffect, Func<ElementData>> elementMap = 
        new Dictionary<ElementEffect, Func<ElementData>>();
    
    public Dictionary<ElementEffect, Func<ElementData>> ElementMap => elementMap;
    
    private void Awake()
    {
        SetElementMap();
    }

    #region Element Methods
    
    private void SetElementMap()
    {
        if (elementData == null) return;
        
        foreach (ElementData element in elementData)
        { 
            elementMap.Add(element.element, () => element);
        }
        
        elementMap.Add(ElementEffect.MatchCurrent, () => 
            GetElementData(Services.Get<PlayerController>()?.pcc?.currentElementEffect ?? ElementEffect.None));
    }
    
    public ElementData GetElementData(ElementEffect elementEffect)
    {
        if (elementMap.TryGetValue(elementEffect, out var elementFunc))
        {
            return elementFunc();
        }
        
        return null;
    }
    
    #endregion
}

public interface IElementSystem : IService
{
    Dictionary<ElementEffect, Func<ElementData>> ElementMap { get; }
    ElementData GetElementData(ElementEffect elementEffect);
}