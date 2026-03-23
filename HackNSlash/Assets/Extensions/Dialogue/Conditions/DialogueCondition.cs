using Extensions.CustomMath.LogicComposition;

namespace Extensions.Dialogue.Conditions
{
    /// <summary>
    /// Base interface for dialogue conditions.
    /// All dialogue conditions should implement this interface.
    /// </summary>
    public interface IDialogueCondition : ICondition<Runtime.DialogueContext>
    {
    }

    /// <summary>
    /// Base abstract class for dialogue conditions.
    /// Provides common functionality for condition implementations.
    /// </summary>
    public abstract class DialogueConditionBase : IDialogueCondition
    {
        public abstract bool Evaluate(Runtime.DialogueContext context);
    }

    /// <summary>
    /// Composite AND condition for combining multiple conditions.
    /// All child conditions must evaluate to true.
    /// </summary>
    public sealed class AndCondition : DialogueConditionBase
    {
        [UnityEngine.SerializeField] private string[] _childConditionReferences = System.Array.Empty<string>();

        public override bool Evaluate(Runtime.DialogueContext context)
        {
            // In a full implementation, deserialize conditions from references
            return true;
        }
    }

    /// <summary>
    /// Composite OR condition for combining multiple conditions.
    /// At least one child condition must evaluate to true.
    /// </summary>
    public sealed class OrCondition : DialogueConditionBase
    {
        [UnityEngine.SerializeField] private string[] _childConditionReferences = System.Array.Empty<string>();

        public override bool Evaluate(Runtime.DialogueContext context)
        {
            // In a full implementation, deserialize conditions from references
            return false;
        }
    }

    /// <summary>
    /// Composite NOT condition for inverting a condition.
    /// </summary>
    public sealed class NotCondition : DialogueConditionBase
    {
        [UnityEngine.SerializeField] private string _childConditionReference = "";

        public override bool Evaluate(Runtime.DialogueContext context)
        {
            // In a full implementation, deserialize condition from reference
            return false;
        }
    }
}

