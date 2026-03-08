using Extensions.EntityComponent;

public interface IDamageableComponent : IComponent
{
    /** <summary>
     * Returns a normalised [0,1] value representing this component's current state for use
     * as a context value in the utility AI.
     * Return 1 when the component's "active" or "full" condition is met, 0 when it is not.
     * </summary>
     */
    float Evaluate();
}