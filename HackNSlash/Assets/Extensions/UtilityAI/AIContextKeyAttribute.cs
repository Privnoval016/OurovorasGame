using System;

namespace Extensions.UtilityAI
{
    /** <summary>
     * Mark any enum with this attribute to make it appear in the <see cref="ConsiderationBases.EnumContextKey"/> dropdown.
     * Only enums tagged here will be shown — keeps the picker focused on keys that are actually meaningful to the AI system.
     * </summary> */
    [AttributeUsage(AttributeTargets.Enum, Inherited = false, AllowMultiple = false)]
    public sealed class AIContextKeyAttribute : Attribute { }
}

