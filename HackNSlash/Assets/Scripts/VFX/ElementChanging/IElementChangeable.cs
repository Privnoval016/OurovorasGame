using System;
using UnityEngine;

public abstract class IElementChangeable
{
    public virtual void Initialize()
    {
        // Optional initialization logic for element changeables
    }
    
    public abstract void UpdateElement(ElementEffect newElement);
    
    public virtual void Cleanup()
    {
        // Optional cleanup logic for element changeables
    }
}

[Serializable]
public struct MaterialInfo
{
    public Material material;
    public Renderer[] targetRenderers;
}