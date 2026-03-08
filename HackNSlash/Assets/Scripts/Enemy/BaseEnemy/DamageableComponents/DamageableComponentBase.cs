using UnityEngine;

/** <summary>
 * Base implementation of IDamageableComponent that returns 0 for all components.
 * This is mainly used for serialization purposes, as Unity cannot serialize interfaces.
 * </summary>
 */
public abstract class DamageableComponentBase : MonoBehaviour, IDamageableComponent
{
    public abstract float Evaluate();
}