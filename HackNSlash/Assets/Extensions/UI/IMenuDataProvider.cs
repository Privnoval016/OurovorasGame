namespace Extensions.UI
{
    /// <summary>
    /// Interface for providing data to UI components without coupling to game logic.
    /// Implementations should retrieve data from backend systems.
    /// </summary>
    public interface IMenuDataProvider<out T>
    {
        /// <summary>
        /// Gets the data from the backend system.
        /// </summary>
        /// <returns>The requested data of type T.</returns>
        T GetData();
    }
}

