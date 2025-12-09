using System;
using System.Collections.Generic;
using UnityEngine;

public static class Services
{
    private static readonly Dictionary<Type, object> ServiceLocator = new Dictionary<System.Type, object>();
    
    public static T Get<T>() where T : IService
    {
        if (ServiceLocator.TryGetValue(typeof(T), out var service))
        {
            return (T)service;
        }
        throw new Exception("Service not found: " + typeof(T));
    }
    
    public static void Register<T>(T service) where T : IService
    {
        ServiceLocator[typeof(T)] = service;
    }
    
    public static void Unregister<T>() where T : IService
    {
        ServiceLocator.Remove(typeof(T));
    }
}