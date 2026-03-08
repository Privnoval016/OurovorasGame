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