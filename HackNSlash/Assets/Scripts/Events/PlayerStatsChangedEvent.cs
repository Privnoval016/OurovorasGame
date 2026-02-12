using Extensions.EventBus;

/// <summary>
/// Event raised when player stats need to be recalculated.
/// This can be triggered by:
/// - Equipping/unequipping items
/// - Unlocking element progress levels
/// - Activating/deactivating skill tree nodes
/// - Leveling up
/// - Any other stat modifier change
/// </summary>
public struct PlayerStatsChangedEvent : IEvent
{
    /// <summary>
    /// The source of the stat change for debugging/logging.
    /// </summary>
    public string Source { get; set; }
    
    /// <summary>
    /// Optional: Specific stat that changed (can be null for "recalculate all").
    /// </summary>
    public string ChangedStat { get; set; }
    
    public PlayerStatsChangedEvent(string source, string changedStat = null)
    {
        Source = source;
        ChangedStat = changedStat;
    }
}

