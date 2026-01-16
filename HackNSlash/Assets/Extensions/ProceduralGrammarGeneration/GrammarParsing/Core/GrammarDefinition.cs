using System;
using System.Collections.Generic;
using System.Linq;

namespace ProceduralGrammarGeneration.GrammarParsing
{
    /// <summary>
    /// The top-level grammar definition containing all symbols and rules.
    /// This is the "source code" that gets compiled into Grammar IR.
    /// </summary>
    [Serializable]
    public class GrammarDefinition
    {
        public string Name { get; set; }
        public List<SymbolDefinition> Symbols { get; set; }
        public List<Rule> Rules { get; set; }
        public SymbolType EntrySymbol { get; set; }

        public GrammarDefinition()
        {
            Symbols = new List<SymbolDefinition>();
            Rules = new List<Rule>();
        }

        public SymbolDefinition GetSymbol(string name)
        {
            return Symbols.FirstOrDefault(s => s.Type.Name == name);
        }

        public SymbolDefinition GetSymbol(SymbolType type)
        {
            return Symbols.FirstOrDefault(s => s.Type.Equals(type));
        }

        public IEnumerable<Rule> GetRulesFor(SymbolType symbol)
        {
            return Rules.Where(r => r.InputSymbol.Equals(symbol));
        }
    }

    /// <summary>
    /// Defines a symbol type with its parameters.
    /// </summary>
    [Serializable]
    public class SymbolDefinition
    {
        public SymbolType Type { get; set; }
        public List<ParameterDefinition> Parameters { get; set; }
        public string Description { get; set; }
        public bool IsTerminal { get; set; } // Terminal symbols don't expand further

        public SymbolDefinition()
        {
            Parameters = new List<ParameterDefinition>();
        }

        public SymbolDefinition(string name, int id, params ParameterDefinition[] parameters)
        {
            Type = new SymbolType(name, id);
            Parameters = new List<ParameterDefinition>(parameters);
        }

        public override string ToString() => $"{Type}({string.Join(", ", Parameters.Select(p => $"{p.Name}: {p.Type}"))})";
    }
}
