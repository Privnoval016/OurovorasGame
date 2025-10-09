public class StatChangeModifierStrategy : IModifierStrategy
{
    private readonly ChangeType changeType;
    private readonly float value;

    public StatChangeModifierStrategy(ChangeType changeType, float value)
    {
        this.changeType = changeType;
        this.value = value;
    }

    public override (int, int) Modify(int baseValue, int currentValue)
    {
        switch (changeType)
        {
            case ChangeType.Flat:
                currentValue += (int)value;
                break;
            case ChangeType.AdditivePercent:
                currentValue += (int)(baseValue * value);
                break;
            case ChangeType.MultiplicativePercent:
                currentValue = (int)(currentValue * (1 + value));
                break;
        }
        return (baseValue, currentValue);
    }
}