using System;
using System.Collections.Generic;
using System.Linq;

namespace ProceduralGrammarGeneration.GrammarParsing
{
    /// <summary>
    /// Represents a node in the derivation tree.
    /// The derivation tree is the result of applying grammar rules - it's the "AST" of execution.
    /// </summary>
    [Serializable]
    public class DerivationNode
    {
        public int Id { get; set; }
        public Symbol Symbol { get; set; }
        public ScopeId Scope { get; set; }
        public DerivationNode Parent { get; set; }
        public List<DerivationNode> Children { get; set; }
        public int RuleId { get; set; } // Rule that produced this node
        public int Depth { get; set; }
        public DerivationStatus Status { get; set; }

        public DerivationNode()
        {
            Children = new List<DerivationNode>();
            Status = DerivationStatus.Pending;
        }

        public DerivationNode(Symbol symbol, ScopeId scope, DerivationNode parent = null)
        {
            Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
            Scope = scope;
            Parent = parent;
            Children = new List<DerivationNode>();
            Depth = parent != null ? parent.Depth + 1 : 0;
            Status = DerivationStatus.Pending;
        }

        public void AddChild(DerivationNode child)
        {
            child.Parent = this;
            child.Depth = Depth + 1;
            Children.Add(child);
        }

        public bool IsLeaf => Children.Count == 0;
        public bool IsRoot => Parent == null;

        public IEnumerable<DerivationNode> GetDescendants()
        {
            foreach (var child in Children)
            {
                yield return child;
                foreach (var descendant in child.GetDescendants())
                    yield return descendant;
            }
        }

        public IEnumerable<DerivationNode> GetLeaves()
        {
            if (IsLeaf)
                yield return this;
            else
                foreach (var child in Children)
                    foreach (var leaf in child.GetLeaves())
                        yield return leaf;
        }

        public override string ToString() => $"{Symbol.Type.Name} ({Scope}) [{Status}]";
    }

    public enum DerivationStatus
    {
        Pending,        // Not yet expanded
        Expanding,      // Currently being expanded
        Expanded,       // Expansion complete
        Terminal,       // Terminal symbol (no further expansion)
        Error           // Error during expansion
    }

    /// <summary>
    /// The complete derivation tree resulting from grammar evaluation.
    /// </summary>
    [Serializable]
    public class DerivationTree
    {
        public DerivationNode Root { get; set; }
        public Dictionary<ScopeId, DerivationNode> NodesByScope { get; set; }
        public Dictionary<int, DerivationNode> NodesById { get; set; }
        public int Seed { get; set; }
        public int MaxDepth { get; set; }
        public int NextNodeId { get; private set; }

        public DerivationTree()
        {
            NodesByScope = new Dictionary<ScopeId, DerivationNode>();
            NodesById = new Dictionary<int, DerivationNode>();
            MaxDepth = 100; // Safety limit
            NextNodeId = 0;
        }

        public DerivationTree(DerivationNode root, int seed = 0)
        {
            Root = root ?? throw new ArgumentNullException(nameof(root));
            NodesByScope = new Dictionary<ScopeId, DerivationNode>();
            NodesById = new Dictionary<int, DerivationNode>();
            Seed = seed;
            MaxDepth = 100;
            NextNodeId = 0;

            RegisterNode(root);
        }

        public int AllocateNodeId()
        {
            return NextNodeId++;
        }

        public void RegisterNode(DerivationNode node)
        {
            if (node.Id == 0)
                node.Id = AllocateNodeId();

            NodesById[node.Id] = node;
            NodesByScope[node.Scope] = node;

            foreach (var child in node.Children)
                RegisterNode(child);
        }

        public DerivationNode GetNode(ScopeId scope)
        {
            return NodesByScope.TryGetValue(scope, out var node) ? node : null;
        }

        public DerivationNode GetNode(int nodeId)
        {
            return NodesById.TryGetValue(nodeId, out var node) ? node : null;
        }

        public IEnumerable<DerivationNode> GetAllNodes()
        {
            if (Root == null) yield break;

            yield return Root;
            foreach (var descendant in Root.GetDescendants())
                yield return descendant;
        }

        public IEnumerable<DerivationNode> GetTerminalNodes()
        {
            return GetAllNodes().Where(n => n.Status == DerivationStatus.Terminal);
        }

        public int GetTotalNodeCount()
        {
            return NodesById.Count;
        }

        public int GetMaxDepth()
        {
            return Root != null ? GetAllNodes().Max(n => n.Depth) : 0;
        }

        public int GetLeafCount()
        {
            return GetAllNodes().Count(n => n.Children.Count == 0);
        }
    }

    /// <summary>
    /// Evaluates a Grammar IR into a derivation tree.
    /// This is the "interpreter" or "executor" of the compiled grammar.
    /// </summary>
    public class DerivationExecutor
    {
        private GrammarIR _grammar;
        private DerivationTree _tree;
        private ScopeRandomizer _randomizer;
        private OverrideManager _overrides;
        private int _maxDepth;
        private int _maxNodes;

        public DerivationExecutor(GrammarIR grammar, int seed = 0, int maxDepth = 100, int maxNodes = 10000)
        {
            _grammar = grammar ?? throw new ArgumentNullException(nameof(grammar));
            _randomizer = new ScopeRandomizer(seed);
            _overrides = new OverrideManager();
            _maxDepth = maxDepth;
            _maxNodes = maxNodes;
        }

        public void SetOverrides(OverrideManager overrides)
        {
            _overrides = overrides ?? new OverrideManager();
        }

        public DerivationTree Execute(Symbol startSymbol, int seed = 0)
        {
            _randomizer = new ScopeRandomizer(seed);
            _tree = new DerivationTree { Seed = seed, MaxDepth = _maxDepth };

            var rootNode = new DerivationNode(startSymbol, ScopeId.Root);
            rootNode.Id = _tree.AllocateNodeId();
            _tree.Root = rootNode;
            _tree.RegisterNode(rootNode);

            ExpandNode(rootNode);

            return _tree;
        }

        private void ExpandNode(DerivationNode node)
        {
            // Check depth limit
            if (node.Depth >= _maxDepth)
            {
                node.Status = DerivationStatus.Terminal;
                return;
            }

            // Check node count limit
            if (_tree.GetTotalNodeCount() >= _maxNodes)
            {
                node.Status = DerivationStatus.Terminal;
                return;
            }

            // Check if symbol is terminal
            if (_grammar.IsTerminal(node.Symbol.Type))
            {
                node.Status = DerivationStatus.Terminal;
                return;
            }

            node.Status = DerivationStatus.Expanding;

            // Find applicable rules
            var rules = _grammar.GetApplicableRules(node.Symbol.Type);
            if (rules == null || rules.Count == 0)
            {
                node.Status = DerivationStatus.Terminal;
                return;
            }

            // Select rule (with condition evaluation and override support)
            var selectedRule = SelectRule(rules, node);
            if (selectedRule == null)
            {
                node.Status = DerivationStatus.Terminal;
                return;
            }

            node.RuleId = selectedRule.Id;

            // Expand according to the rule
            try
            {
                ExpandWithRule(node, selectedRule);
                node.Status = DerivationStatus.Expanded;

                // Recursively expand children
                foreach (var child in node.Children)
                {
                    ExpandNode(child);
                }
            }
            catch (Exception ex)
            {
                node.Status = DerivationStatus.Error;
                throw new DerivationException($"Error expanding node {node.Scope}: {ex.Message}", ex);
            }
        }

        private RuleIR SelectRule(List<RuleIR> rules, DerivationNode node)
        {
            var context = BuildParameterContext(node.Symbol);

            // Filter rules by condition
            var applicableRules = rules.Where(r =>
                r.Condition == null || r.Condition.Evaluate(context)
            ).ToList();

            if (applicableRules.Count == 0)
                return null;

            // Check for override
            var overrideRuleId = _overrides.GetRuleOverride(node.Scope);
            if (overrideRuleId.HasValue)
            {
                var overrideRule = applicableRules.FirstOrDefault(r => r.Id == overrideRuleId.Value);
                if (overrideRule != null)
                    return overrideRule;
            }

            // Use first applicable rule (highest priority)
            return applicableRules[0];
        }

        private void ExpandWithRule(DerivationNode node, RuleIR rule)
        {
            var context = BuildParameterContext(node.Symbol);
            ExpandExpansion(node, rule.Expansion, context, 0);
        }

        private void ExpandExpansion(DerivationNode parent, ExpansionIR expansion, Dictionary<string, object> context, int childIndexOffset)
        {
            switch (expansion)
            {
                case ProductionExpansionIR prod:
                    ExpandProduction(parent, prod, context, childIndexOffset);
                    break;

                case SplitExpansionIR split:
                    ExpandSplit(parent, split, context, childIndexOffset);
                    break;

                case RepeatExpansionIR repeat:
                    ExpandRepeat(parent, repeat, context, childIndexOffset);
                    break;

                case ChooseExpansionIR choose:
                    ExpandChoose(parent, choose, context, childIndexOffset);
                    break;
            }
        }

        private void ExpandProduction(DerivationNode parent, ProductionExpansionIR prod, Dictionary<string, object> context, int childIndexOffset)
        {
            int index = childIndexOffset;
            foreach (var symbolInstance in prod.Symbols)
            {
                var childSymbol = InstantiateSymbol(symbolInstance, context);
                var childScope = new ScopeId(parent.Scope, childSymbol.Type.Name, index);
                var childNode = new DerivationNode(childSymbol, childScope);
                childNode.Id = _tree.AllocateNodeId();
                parent.AddChild(childNode);
                _tree.RegisterNode(childNode);
                index++;
            }
        }

        private void ExpandSplit(DerivationNode parent, SplitExpansionIR split, Dictionary<string, object> context, int childIndexOffset)
        {
            int index = childIndexOffset;
            foreach (var part in split.Parts)
            {
                var partScope = new ScopeId(parent.Scope, $"Split{split.Axis}", index);
                var partNode = new DerivationNode(parent.Symbol, partScope);
                partNode.Id = _tree.AllocateNodeId();
                parent.AddChild(partNode);
                _tree.RegisterNode(partNode);

                ExpandExpansion(partNode, part.Expansion, context, 0);
                index++;
            }
        }

        private void ExpandRepeat(DerivationNode parent, RepeatExpansionIR repeat, Dictionary<string, object> context, int childIndexOffset)
        {
            var sizeValue = repeat.Size.Evaluate(context);
            if (!(sizeValue is float || sizeValue is int))
                return;

            int count = Convert.ToInt32(sizeValue);
            count = Math.Min(count, 1000); // Safety limit

            for (int i = 0; i < count; i++)
            {
                var repeatScope = new ScopeId(parent.Scope, $"Repeat{repeat.Axis}", childIndexOffset + i);
                var repeatNode = new DerivationNode(parent.Symbol, repeatScope);
                repeatNode.Id = _tree.AllocateNodeId();
                parent.AddChild(repeatNode);
                _tree.RegisterNode(repeatNode);

                ExpandExpansion(repeatNode, repeat.Content, context, 0);
            }
        }

        private void ExpandChoose(DerivationNode parent, ChooseExpansionIR choose, Dictionary<string, object> context, int childIndexOffset)
        {
            // Check for override
            var overrideIndex = _overrides.GetChoiceOverride(parent.Scope, parent.RuleId);
            int selectedIndex;

            if (overrideIndex.HasValue)
            {
                // User override
                selectedIndex = overrideIndex.Value;
            }
            else if (choose.DefaultOptionIndex >= 0)
            {
                // Default option
                selectedIndex = choose.DefaultOptionIndex;
            }
            else
            {
                // Weighted random selection
                selectedIndex = SelectWeightedOption(choose, parent.Scope);
            }

            if (selectedIndex >= 0 && selectedIndex < choose.Options.Count)
            {
                var selectedOption = choose.Options[selectedIndex];
                ExpandExpansion(parent, selectedOption.Expansion, context, childIndexOffset);
            }
        }

        private int SelectWeightedOption(ChooseExpansionIR choose, ScopeId scope)
        {
            if (choose.TotalWeight <= 0)
                return 0; // Fallback to first option

            var random = _randomizer.GetRandom(scope, 0); // ruleId = 0 for simplicity
            var value = random * choose.TotalWeight;

            for (int i = 0; i < choose.Options.Count; i++)
            {
                if (value <= choose.Options[i].CumulativeWeight)
                    return i;
            }

            return choose.Options.Count - 1;
        }

        private Symbol InstantiateSymbol(SymbolInstanceIR instance, Dictionary<string, object> context)
        {
            var symbol = new Symbol(instance.Type);

            foreach (var kvp in instance.ParameterExpressions)
            {
                // Get the parameter name from the instance
                if (!instance.ParameterNames.TryGetValue(kvp.Key, out string paramName))
                {
                    continue;
                }
                
                // Evaluate the expression to get the value
                var value = kvp.Value.Evaluate(context);
                symbol.Parameters[paramName] = value;
            }

            return symbol;
        }

        private Dictionary<string, object> BuildParameterContext(Symbol symbol)
        {
            return new Dictionary<string, object>(symbol.Parameters);
        }
    }

    /// <summary>
    /// Exception thrown during derivation execution.
    /// </summary>
    public class DerivationException : Exception
    {
        public DerivationException(string message) : base(message) { }
        public DerivationException(string message, Exception innerException) : base(message, innerException) { }
    }
}
