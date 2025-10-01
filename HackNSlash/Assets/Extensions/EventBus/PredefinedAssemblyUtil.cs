using System;
using System.Collections.Generic;
using System.Reflection;

namespace Extensions.EventBus
{
    /**
     * <summary>
     * This utility class provides methods to retrieve types from predefined assemblies in a Unity project.
     * It focuses on the common assemblies used in Unity projects, such as Assembly-CSharp and Assembly-CSharp-Editor.
     * The main functionality is to find and return all types that implement a specified interface from these assemblies.
     * </summary>
     */
    public static class PredefinedAssemblyUtil
    {
        /**
         * <summary>
         * Enumeration of predefined assembly types in a Unity project.
         * These assemblies are commonly used for organizing scripts in Unity.
         * </summary>
         */
        enum AssemblyType
        {
            AssemblyCSharp,
            AssemblyCSharpEditor,
            AssemblyCSharpEditorFirstPass,
            AssemblyCSharpFirstPass
        }

        /**
         * <summary>
         * Maps assembly names to their corresponding AssemblyType enum values.
         * Returns null if the assembly name does not match any predefined types.
         * </summary>
         */
        static AssemblyType? GetAssemblyType(string assemblyName)
        {
            return assemblyName switch
            {
                "Assembly-CSharp" => AssemblyType.AssemblyCSharp,
                "Assembly-CSharp-Editor" => AssemblyType.AssemblyCSharpEditor,
                "Assembly-CSharp-Editor-firstpass" => AssemblyType.AssemblyCSharpEditorFirstPass,
                "Assembly-CSharp-firstpass" => AssemblyType.AssemblyCSharpFirstPass,
                _ => null
            };
        }
        
        /**
         * <summary>
         * Adds types from the provided assembly types that implement the specified interface type to the given collection.
         * </summary>
         */
        private static void AddTypesFromAssembly(Type[] assemblyTypes, Type interfaceType, ICollection<Type> types)
        {
            if (assemblyTypes == null) return;
            foreach (var type in assemblyTypes)
            {
                if (type != interfaceType && interfaceType.IsAssignableFrom(type))
                {
                    types.Add(type);
                }
            }
        }

        /**
         * <summary>
         * Retrieves all types from predefined Unity assemblies that implement the specified interface type.
         * The method scans the current AppDomain for assemblies, filters them based on predefined names,
         * and collects types that implement the given interface.
         * </summary>
         */
        public static List<Type> GetTypes(Type interfaceType)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            
            Dictionary<AssemblyType, Type[]> assemblyTypes = new();
            List<Type> types = new();
            foreach (var assembly in assemblies)
            {
                var assemblyType = GetAssemblyType(assembly.GetName().Name);
                if (assemblyType != null)
                {
                    assemblyTypes.Add((AssemblyType) assemblyType, assembly.GetTypes());
                }
            }
            
            AddTypesFromAssembly(assemblyTypes[AssemblyType.AssemblyCSharp], interfaceType, types);
            AddTypesFromAssembly(assemblyTypes[AssemblyType.AssemblyCSharpFirstPass], interfaceType, types);
            AddTypesFromAssembly(assemblyTypes[AssemblyType.AssemblyCSharpEditor], interfaceType, types);
            AddTypesFromAssembly(assemblyTypes[AssemblyType.AssemblyCSharpEditorFirstPass], interfaceType, types);
            
            return types;
        }
    }
}