using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;

namespace Extensions.CustomMath.LogicComposition
{
    public interface ICondition<in TContext>
    {
        bool Evaluate(TContext context);
        
        #region Odin Dropdown Helpers
        
        public static IEnumerable<ValueDropdownItem> GetBroadcastStrategies()
        {
            // Top-priority composites first
            yield return new ValueDropdownItem("* AND Condition *", new AndCondition<QuestEventData>());
            yield return new ValueDropdownItem("* OR Condition *", new OrCondition<QuestEventData>());
            yield return new ValueDropdownItem("* NOT Condition *", new NotCondition<QuestEventData>());

            // Dynamically add all other condition types
            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => typeof(ICondition<QuestEventData>).IsAssignableFrom(t)
                            && !t.IsAbstract
                            && t != typeof(AndCondition<QuestEventData>)
                            && t != typeof(OrCondition<QuestEventData>)
                            && t != typeof(NotCondition<QuestEventData>));

            foreach (var t in allTypes)
            {
                yield return new ValueDropdownItem(t.Name, (ICondition<QuestEventData>)Activator.CreateInstance(t));
            }
        }
        
        #endregion
    }
}