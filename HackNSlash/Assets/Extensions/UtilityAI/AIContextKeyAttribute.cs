using System;
namespace Extensions.UtilityAI
{
    /** <summary>
     * Tag a <c>static class</c> containing <see cref="ContextKey{TValue}"/> fields with this attribute
     * to make all its keys appear in the <see cref="ContextKeyField"/> inspector dropdown.
     * Only classes tagged here are scanned — keeps the picker focused on keys meaningful to the AI.
     * </summary>
     */
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class AIContextKeyAttribute : Attribute { }
}
