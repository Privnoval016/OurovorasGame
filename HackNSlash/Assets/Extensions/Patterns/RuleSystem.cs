using System;
using System.Collections.Generic;

namespace Extensions.Patterns
{
    public class RuleSystem<TContext, TResult>
    {
        private readonly Dictionary<Type, IRuleBucket> _buckets = new Dictionary<Type, IRuleBucket>();
        
        public void AddRule<TEvent>(IRule<TEvent, TContext, TResult> rule)
        {
            var eventType = typeof(TEvent);
            if (!_buckets.TryGetValue(eventType, out var bucket))
            {
                bucket = new RuleBucket<TEvent, TContext, TResult>();
                _buckets[eventType] = bucket;
            }

            bucket.As<TEvent, TContext, TResult>().Rules.Add(rule);
        }
        
        public TResult ApplyRules<TEvent>(TEvent eventData, TContext context, Func<TResult, TResult, TResult> combiner)
        {
            var eventType = typeof(TEvent);
            if (!_buckets.TryGetValue(eventType, out var bucket))
            {
                return default;
            }
            
            var ruleBucket = bucket.As<TEvent, TContext, TResult>();
            TResult result = default;
            bool initialized = false;
            
            foreach (var rule in ruleBucket.Rules)
            {
                if (!rule.IsMatch(eventData, context)) continue;
                
                var ruleResult = rule.Apply(eventData, context);
                
                if (!initialized)
                {
                    result = ruleResult;
                    initialized = true;
                }
                else
                {
                    result = combiner(result, ruleResult);
                }
            }
            
            return result;
        }
    }
}