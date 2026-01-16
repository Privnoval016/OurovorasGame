using System;
using System.Collections.Generic;
using System.Linq;

namespace ProceduralGrammarGeneration.GrammarParsing
{
    /// <summary>
    /// Manages user overrides for rule and choice selections.
    /// Overrides allow manual collapse of ambiguity at specific scopes.
    /// Priority: User Override > Default > Random
    /// </summary>
    [Serializable]
    public class OverrideManager
    {
        // Scope -> RuleId (which rule to use for this scope)
        private Dictionary<ScopeId, int> _ruleOverrides;

        // (Scope, RuleId) -> ChoiceIndex (which choice option to select)
        private Dictionary<OverrideKey, int> _choiceOverrides;

        public OverrideManager()
        {
            _ruleOverrides = new Dictionary<ScopeId, int>();
            _choiceOverrides = new Dictionary<OverrideKey, int>();
        }

        #region Rule Overrides

        /// <summary>
        /// Sets which rule should be used at a specific scope.
        /// </summary>
        public void SetRuleOverride(ScopeId scope, int ruleId)
        {
            _ruleOverrides[scope] = ruleId;
        }

        /// <summary>
        /// Gets the overridden rule ID for a scope, if any.
        /// </summary>
        public int? GetRuleOverride(ScopeId scope)
        {
            return _ruleOverrides.TryGetValue(scope, out var ruleId) ? ruleId : (int?)null;
        }

        /// <summary>
        /// Removes a rule override at a specific scope.
        /// </summary>
        public bool RemoveRuleOverride(ScopeId scope)
        {
            return _ruleOverrides.Remove(scope);
        }

        /// <summary>
        /// Checks if a rule override exists at a scope.
        /// </summary>
        public bool HasRuleOverride(ScopeId scope)
        {
            return _ruleOverrides.ContainsKey(scope);
        }

        #endregion

        #region Choice Overrides

        /// <summary>
        /// Sets which choice option should be selected at a specific scope.
        /// </summary>
        public void SetChoiceOverride(ScopeId scope, int ruleId, int choiceIndex)
        {
            var key = new OverrideKey(scope, ruleId);
            _choiceOverrides[key] = choiceIndex;
        }

        /// <summary>
        /// Gets the overridden choice index for a scope and rule, if any.
        /// </summary>
        public int? GetChoiceOverride(ScopeId scope, int ruleId)
        {
            var key = new OverrideKey(scope, ruleId);
            return _choiceOverrides.TryGetValue(key, out var index) ? index : (int?)null;
        }

        /// <summary>
        /// Removes a choice override at a specific scope.
        /// </summary>
        public bool RemoveChoiceOverride(ScopeId scope, int ruleId)
        {
            var key = new OverrideKey(scope, ruleId);
            return _choiceOverrides.Remove(key);
        }

        /// <summary>
        /// Checks if a choice override exists at a scope.
        /// </summary>
        public bool HasChoiceOverride(ScopeId scope, int ruleId)
        {
            var key = new OverrideKey(scope, ruleId);
            return _choiceOverrides.ContainsKey(key);
        }

        #endregion

        #region Bulk Operations

        /// <summary>
        /// Clears all overrides.
        /// </summary>
        public void ClearAll()
        {
            _ruleOverrides.Clear();
            _choiceOverrides.Clear();
        }

        /// <summary>
        /// Clears all overrides within a specific scope subtree.
        /// </summary>
        public void ClearScopeSubtree(ScopeId rootScope)
        {
            var scopePrefix = rootScope.Path;
            
            var rulesToRemove = _ruleOverrides.Keys
                .Where(scope => scope.Path.StartsWith(scopePrefix))
                .ToList();

            foreach (var scope in rulesToRemove)
                _ruleOverrides.Remove(scope);

            var choicesToRemove = _choiceOverrides.Keys
                .Where(key => key.Scope.Path.StartsWith(scopePrefix))
                .ToList();

            foreach (var key in choicesToRemove)
                _choiceOverrides.Remove(key);
        }

        /// <summary>
        /// Gets all rule overrides as a list.
        /// </summary>
        public List<RuleOverride> GetAllRuleOverrides()
        {
            return _ruleOverrides.Select(kvp => new RuleOverride
            {
                Scope = kvp.Key,
                RuleId = kvp.Value
            }).ToList();
        }

        /// <summary>
        /// Gets all choice overrides as a list.
        /// </summary>
        public List<ChoiceOverride> GetAllChoiceOverrides()
        {
            return _choiceOverrides.Select(kvp => new ChoiceOverride
            {
                Scope = kvp.Key.Scope,
                RuleId = kvp.Key.RuleId,
                ChoiceIndex = kvp.Value
            }).ToList();
        }

        /// <summary>
        /// Gets the total number of overrides.
        /// </summary>
        public int GetOverrideCount()
        {
            return _ruleOverrides.Count + _choiceOverrides.Count;
        }

        #endregion

        #region Serialization Support

        /// <summary>
        /// Exports overrides to a serializable format.
        /// </summary>
        public OverrideData Export()
        {
            return new OverrideData
            {
                RuleOverrides = GetAllRuleOverrides(),
                ChoiceOverrides = GetAllChoiceOverrides()
            };
        }

        /// <summary>
        /// Imports overrides from a serializable format.
        /// </summary>
        public void Import(OverrideData data)
        {
            ClearAll();

            foreach (var ruleOverride in data.RuleOverrides)
                SetRuleOverride(ruleOverride.Scope, ruleOverride.RuleId);

            foreach (var choiceOverride in data.ChoiceOverrides)
                SetChoiceOverride(choiceOverride.Scope, choiceOverride.RuleId, choiceOverride.ChoiceIndex);
        }

        #endregion

        /// <summary>
        /// Composite key for choice overrides.
        /// </summary>
        [Serializable]
        private struct OverrideKey : IEquatable<OverrideKey>
        {
            public ScopeId Scope { get; }
            public int RuleId { get; }

            public OverrideKey(ScopeId scope, int ruleId)
            {
                Scope = scope;
                RuleId = ruleId;
            }

            public bool Equals(OverrideKey other) =>
                Scope.Equals(other.Scope) && RuleId == other.RuleId;

            public override bool Equals(object obj) =>
                obj is OverrideKey other && Equals(other);

            public override int GetHashCode() =>
                HashCode.Combine(Scope, RuleId);
        }
    }

    #region Override Data Structures

    /// <summary>
    /// Represents a rule override for serialization.
    /// </summary>
    [Serializable]
    public class RuleOverride
    {
        public ScopeId Scope { get; set; }
        public int RuleId { get; set; }

        public override string ToString() => $"Rule override at {Scope}: Rule {RuleId}";
    }

    /// <summary>
    /// Represents a choice override for serialization.
    /// </summary>
    [Serializable]
    public class ChoiceOverride
    {
        public ScopeId Scope { get; set; }
        public int RuleId { get; set; }
        public int ChoiceIndex { get; set; }

        public override string ToString() => $"Choice override at {Scope} (Rule {RuleId}): Option {ChoiceIndex}";
    }

    /// <summary>
    /// Serializable container for all override data.
    /// </summary>
    [Serializable]
    public class OverrideData
    {
        public List<RuleOverride> RuleOverrides { get; set; }
        public List<ChoiceOverride> ChoiceOverrides { get; set; }

        public OverrideData()
        {
            RuleOverrides = new List<RuleOverride>();
            ChoiceOverrides = new List<ChoiceOverride>();
        }
    }

    #endregion

    /// <summary>
    /// Provides deterministic, scope-based randomization.
    /// Guarantees that:
    /// - Same seed + same scope = same random value
    /// - Rebuilding produces identical results
    /// - Partial regeneration doesn't affect other scopes
    /// </summary>
    public class ScopeRandomizer
    {
        private readonly int _globalSeed;
        private readonly Dictionary<int, Random> _randomCache;

        public ScopeRandomizer(int globalSeed = 0)
        {
            _globalSeed = globalSeed;
            _randomCache = new Dictionary<int, Random>();
        }

        /// <summary>
        /// Gets a deterministic random value [0, 1) for a specific scope and rule.
        /// </summary>
        public float GetRandom(ScopeId scope, int ruleId)
        {
            var random = GetRandomGenerator(scope, ruleId);
            return (float)random.NextDouble();
        }

        /// <summary>
        /// Gets a deterministic random integer in range [min, max) for a specific scope and rule.
        /// </summary>
        public int GetRandomInt(ScopeId scope, int ruleId, int min, int max)
        {
            var random = GetRandomGenerator(scope, ruleId);
            return random.Next(min, max);
        }

        /// <summary>
        /// Gets a random generator for a specific scope and rule.
        /// </summary>
        private Random GetRandomGenerator(ScopeId scope, int ruleId)
        {
            var seed = ComputeSeed(scope, ruleId);

            if (!_randomCache.TryGetValue(seed, out var random))
            {
                random = new Random(seed);
                _randomCache[seed] = random;
            }

            return random;
        }

        /// <summary>
        /// Computes a deterministic seed from global seed, scope, and rule.
        /// </summary>
        private int ComputeSeed(ScopeId scope, int ruleId)
        {
            unchecked
            {
                int hash = _globalSeed;
                hash = (hash * 397) ^ scope.Hash;
                hash = (hash * 397) ^ ruleId;
                return hash;
            }
        }

        /// <summary>
        /// Clears the random generator cache.
        /// </summary>
        public void ClearCache()
        {
            _randomCache.Clear();
        }

        /// <summary>
        /// Gets the current global seed.
        /// </summary>
        public int GetGlobalSeed() => _globalSeed;
    }

    /// <summary>
    /// Helper struct for C# versions that don't have HashCode.Combine.
    /// </summary>
    internal static class HashCode
    {
        public static int Combine<T1, T2>(T1 value1, T2 value2)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (value1?.GetHashCode() ?? 0);
                hash = hash * 31 + (value2?.GetHashCode() ?? 0);
                return hash;
            }
        }
    }
}
