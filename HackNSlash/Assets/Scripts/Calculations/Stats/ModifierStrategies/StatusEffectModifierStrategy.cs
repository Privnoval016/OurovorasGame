public class StatusEffectModifierStrategy : IModifierStrategy
{
    private int stacksToAdd;
    
    public StatusEffectModifierStrategy(int stacksToAdd)
    {
        this.stacksToAdd = stacksToAdd;
    }
    
    public (int, int) Modify(int baseValue, int currentValue)
    {
        currentValue += stacksToAdd;
        if (currentValue < 0) currentValue = 0;
        return (baseValue, currentValue);
    }
}