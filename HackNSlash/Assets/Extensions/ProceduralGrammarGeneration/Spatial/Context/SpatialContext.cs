using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGrammarGeneration.Spatial
{
    /// <summary>
    /// Tracks spatial state during interpretation - current position, orientation, and context variables.
    /// This is passed through the spatial traversal and updated as nodes are placed.
    /// </summary>
    public class SpatialContext
    {
        /// <summary>
        /// Current position in world space (updated as we place nodes)
        /// </summary>
        public Vector3 CurrentPosition { get; set; }
        
        /// <summary>
        /// Current orientation in world space
        /// </summary>
        public Quaternion CurrentOrientation { get; set; }
        
        /// <summary>
        /// Current local space basis (right, up, forward vectors)
        /// </summary>
        public Vector3 Right => CurrentOrientation * Vector3.right;
        public Vector3 Up => CurrentOrientation * Vector3.up;
        public Vector3 Forward => CurrentOrientation * Vector3.forward;
        
        /// <summary>
        /// Stack of transform states for hierarchical traversal
        /// </summary>
        public Stack<TransformState> TransformStack { get; private set; }
        
        /// <summary>
        /// Context variables (can store spline curves, constraints, custom data)
        /// </summary>
        public Dictionary<string, object> Variables { get; set; }
        
        /// <summary>
        /// Parent node being processed (for hierarchical placement)
        /// </summary>
        public SpatialNode CurrentParent { get; set; }
        
        /// <summary>
        /// Counter for generating unique IDs
        /// </summary>
        public int NodeIdCounter { get; set; }

        public SpatialContext()
        {
            CurrentPosition = Vector3.zero;
            CurrentOrientation = Quaternion.identity;
            TransformStack = new Stack<TransformState>();
            Variables = new Dictionary<string, object>();
            NodeIdCounter = 0;
        }
        
        /// <summary>
        /// Push current transform state onto stack
        /// </summary>
        public void PushTransform()
        {
            TransformStack.Push(new TransformState
            {
                Position = CurrentPosition,
                Orientation = CurrentOrientation
            });
        }
        
        /// <summary>
        /// Pop transform state from stack
        /// </summary>
        public void PopTransform()
        {
            if (TransformStack.Count > 0)
            {
                var state = TransformStack.Pop();
                CurrentPosition = state.Position;
                CurrentOrientation = state.Orientation;
            }
        }
        
        /// <summary>
        /// Move along the current forward direction
        /// </summary>
        public void Advance(float distance)
        {
            CurrentPosition += Forward * distance;
        }
        
        /// <summary>
        /// Move along the current right direction
        /// </summary>
        public void StrafeRight(float distance)
        {
            CurrentPosition += Right * distance;
        }
        
        /// <summary>
        /// Move along the current up direction
        /// </summary>
        public void Elevate(float distance)
        {
            CurrentPosition += Up * distance;
        }
        
        /// <summary>
        /// Rotate around current up axis
        /// </summary>
        public void RotateYaw(float degrees)
        {
            CurrentOrientation *= Quaternion.Euler(0, degrees, 0);
        }
        
        /// <summary>
        /// Rotate around current right axis
        /// </summary>
        public void RotatePitch(float degrees)
        {
            CurrentOrientation *= Quaternion.Euler(degrees, 0, 0);
        }
        
        /// <summary>
        /// Rotate around current forward axis
        /// </summary>
        public void RotateRoll(float degrees)
        {
            CurrentOrientation *= Quaternion.Euler(0, 0, degrees);
        }
        
        /// <summary>
        /// Get a variable with type safety
        /// </summary>
        public T GetVariable<T>(string name, T defaultValue = default)
        {
            if (Variables.TryGetValue(name, out var value) && value is T typedValue)
                return typedValue;
            return defaultValue;
        }
        
        /// <summary>
        /// Set a context variable
        /// </summary>
        public void SetVariable(string name, object value)
        {
            Variables[name] = value;
        }
        
        /// <summary>
        /// Clone the current context
        /// </summary>
        public SpatialContext Clone()
        {
            var clone = new SpatialContext
            {
                CurrentPosition = CurrentPosition,
                CurrentOrientation = CurrentOrientation,
                CurrentParent = CurrentParent,
                NodeIdCounter = NodeIdCounter
            };
            
            foreach (var kvp in Variables)
                clone.Variables[kvp.Key] = kvp.Value;
            
            return clone;
        }
    }
    
    /// <summary>
    /// Saved transform state for hierarchical traversal
    /// </summary>
    public struct TransformState
    {
        public Vector3 Position;
        public Quaternion Orientation;
    }
}
