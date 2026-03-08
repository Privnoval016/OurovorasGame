using System;
using Extensions.EntityComponent;

public class ElementComponent : IDamageableComponent
{
    public WrappedField<ElementEffect> CurrentElementEffect { get; private set; }

    public ElementComponent(Func<ElementEffect> onElementChange)
    {
        CurrentElementEffect = new WrappedField<ElementEffect>(onElementChange);
    }

    /** <summary>Returns 1 if the element is not None, 0 if it is None.</summary> */
    public float Evaluate() => CurrentElementEffect.Value != ElementEffect.None ? 1f : 0f;
}