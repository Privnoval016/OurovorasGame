public class StyleResult
{
    public readonly float StylePoints;
    public readonly bool BreakCombo;
    public readonly bool ForceMaxRank;
    
    public StyleResult(float stylePoints, bool breakCombo = false, bool forceMaxRank = false)
    {
        StylePoints = stylePoints;
        BreakCombo = breakCombo;
        ForceMaxRank = forceMaxRank;
    }
    
    public static StyleResult operator +(StyleResult a, StyleResult b)
    {
        return new StyleResult(
            a.StylePoints + b.StylePoints,
            a.BreakCombo || b.BreakCombo,
            a.ForceMaxRank || b.ForceMaxRank
        );
    }
    
    public static StyleResult None => new StyleResult(0f);
}