using System;
using UnityEngine;

namespace Extensions.UtilityAI.ConsiderationBases
{
    /** <summary>Abstract base for all Consideration ScriptableObjects.</summary> */
    public abstract class Consideration : ScriptableObject
    {
        /** <summary>Evaluate the context and return a [0,1] utility score.</summary> */
        public abstract float Evaluate(IContextBase context);
    }

    /**
     * <summary>
     * Serialisable struct that stores any enum value without requiring a concrete wrapper subclass.
     * Stores the enum's fully-qualified type name and the integer value of the selected member.
     * The <c>EnumContextKeyDrawer</c> renders this as a two-column popup in the Inspector.
     * </summary>
     */
    [Serializable]
    public struct EnumContextKey
    {
        [SerializeField] public string enumTypeName;
        [SerializeField] public int enumValue;

        /** <summary>Returns the boxed enum value for use with <see cref="IContextBase"/>. Returns null if the type cannot be resolved.</summary> */
        public object GetKey()
        {
            if (string.IsNullOrEmpty(enumTypeName)) return null;
            Type t = ResolveType();
            return t != null ? Enum.ToObject(t, enumValue) : null;
        }

        /** <summary>Resolves and returns the enum <see cref="Type"/>, or null if not found in any loaded assembly.</summary> */
        public Type ResolveType()
        {
            if (string.IsNullOrEmpty(enumTypeName)) return null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = asm.GetType(enumTypeName);
                if (t != null && t.IsEnum) return t;
            }
            return null;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(enumTypeName)) return "(none)";
            Type t = ResolveType();
            if (t == null) return $"({enumTypeName}?).{enumValue}";
            return $"{t.Name}.{Enum.GetName(t, enumValue) ?? enumValue.ToString()}";
        }

        // Legacy overload kept so old call sites (ConsiderationKey pattern) still compile during migration.
        public object GetKey(out Type enumType)
        {
            enumType = ResolveType();
            return GetKey();
        }
    }

    /** <summary>Legacy base class. Kept so existing <c>EnemyConsiderationKey</c> subclasses compile during migration. Use <see cref="EnumContextKey"/> for new code.</summary> */
    [Obsolete("Use EnumContextKey instead.")]
    public abstract class ConsiderationKey
    {
        public abstract object GetKey(out Type enumType);
    }
}
