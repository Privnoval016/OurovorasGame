using System;

namespace Extensions.UI
{
    /// <summary>
    /// Interface for components that can handle input independently.
    /// Allows input handling to be assigned externally for modularity.
    /// </summary>
    public interface IInputReceiver
    {
        /// <summary>
        /// Subscribe to input events from an input source.
        /// </summary>
        void SubscribeToInput();
        
        /// <summary>
        /// Unsubscribe from input events.
        /// </summary>
        void UnsubscribeFromInput();
    }
    
    /// <summary>
    /// Interface for handling navigation input (e.g., menu scrolling, button selection).
    /// </summary>
    public interface INavigationReceiver : IInputReceiver
    {
        /// <summary>
        /// Called when navigation input is received.
        /// </summary>
        /// <param name="direction">The navigation direction vector.</param>
        void OnNavigate(UnityEngine.Vector2 direction);
        
        /// <summary>
        /// Called when selection input is received.
        /// </summary>
        void OnSelect();
        
        /// <summary>
        /// Called when back/cancel input is received.
        /// </summary>
        void OnBack();
    }
}

