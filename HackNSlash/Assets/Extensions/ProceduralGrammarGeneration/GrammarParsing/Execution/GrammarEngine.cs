using System;
using System.Text;

namespace ProceduralGrammarGeneration.GrammarParsing
{
    /// <summary>
    /// The main entry point for the grammar compilation pipeline.
    /// Coordinates lexing, parsing, semantic analysis, and compilation.
    /// </summary>
    public class GrammarCompilerPipeline
    {
        private string _source;
        private CompilationOptions _options;

        public GrammarCompilerPipeline(string source, CompilationOptions options = null)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _options = options ?? new CompilationOptions();
        }

        /// <summary>
        /// Compiles grammar source text into optimized Grammar IR.
        /// Returns a result containing the IR or compilation errors.
        /// </summary>
        public CompilationResult Compile()
        {
            var result = new CompilationResult();

            try
            {
                // Stage 1: Lexing
                result.AddStage("Lexing");
                var lexer = new GrammarLexer(_source);
                var tokens = lexer.Tokenize();
                result.CompleteStage("Lexing", $"{tokens.Count} tokens");

                // Stage 2: Parsing
                result.AddStage("Parsing");
                var parser = new GrammarParser(tokens);
                var grammar = parser.Parse();
                result.CompleteStage("Parsing", $"{grammar.Rules.Count} rules, {grammar.Symbols.Count} symbols");

                // Stage 3: Semantic Analysis
                result.AddStage("Semantic Analysis");
                var analyzer = new SemanticAnalyzer(grammar);
                var semanticResult = analyzer.Analyze();
                
                result.Errors.AddRange(semanticResult.Errors);
                result.Warnings.AddRange(semanticResult.Warnings);

                if (!semanticResult.IsValid)
                {
                    result.Success = false;
                    result.CompleteStage("Semantic Analysis", $"{semanticResult.Errors.Count} errors");
                    return result;
                }

                result.CompleteStage("Semantic Analysis", $"{semanticResult.Warnings.Count} warnings");

                // Stage 4: IR Compilation
                result.AddStage("IR Compilation");
                var compiler = new GrammarCompiler(grammar);
                var ir = compiler.Compile();
                result.CompleteStage("IR Compilation", $"{ir.RulesById.Count} rules compiled");

                result.Success = true;
                result.GrammarIR = ir;
                result.GrammarDefinition = grammar;
            }
            catch (LexerException ex)
            {
                result.Success = false;
                result.Errors.Add(new SemanticError { Message = $"Lexer error: {ex.Message}" });
            }
            catch (ParserException ex)
            {
                result.Success = false;
                result.Errors.Add(new SemanticError { Message = $"Parser error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add(new SemanticError { Message = $"Compilation error: {ex.Message}" });
            }

            return result;
        }

        /// <summary>
        /// Quick validation without full compilation.
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            var result = Compile();
            if (result.Success)
            {
                errorMessage = null;
                return true;
            }

            var sb = new StringBuilder();
            foreach (var error in result.Errors)
                sb.AppendLine(error.ToString());

            errorMessage = sb.ToString();
            return false;
        }
    }

    /// <summary>
    /// Options for grammar compilation.
    /// </summary>
    public class CompilationOptions
    {
        public bool StrictMode { get; set; } = true;
        public bool OptimizeIR { get; set; } = true;
        public bool GenerateDebugInfo { get; set; } = false;
        public int MaxWarnings { get; set; } = 100;
    }

    /// <summary>
    /// Result of grammar compilation.
    /// </summary>
    public class CompilationResult
    {
        public bool Success { get; set; }
        public GrammarIR GrammarIR { get; set; }
        public GrammarDefinition GrammarDefinition { get; set; }
        public System.Collections.Generic.List<SemanticError> Errors { get; set; }
        public System.Collections.Generic.List<SemanticWarning> Warnings { get; set; }
        public System.Collections.Generic.List<CompilationStage> Stages { get; set; }

        public CompilationResult()
        {
            Errors = new System.Collections.Generic.List<SemanticError>();
            Warnings = new System.Collections.Generic.List<SemanticWarning>();
            Stages = new System.Collections.Generic.List<CompilationStage>();
        }

        public void AddStage(string name)
        {
            Stages.Add(new CompilationStage
            {
                Name = name,
                StartTime = DateTime.Now
            });
        }

        public void CompleteStage(string name, string details = null)
        {
            var stage = Stages.Find(s => s.Name == name);
            if (stage != null)
            {
                stage.EndTime = DateTime.Now;
                stage.Details = details;
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Compilation {(Success ? "succeeded" : "failed")}");
            
            if (Errors.Count > 0)
            {
                sb.AppendLine($"\nErrors ({Errors.Count}):");
                foreach (var error in Errors)
                    sb.AppendLine($"  {error}");
            }

            if (Warnings.Count > 0)
            {
                sb.AppendLine($"\nWarnings ({Warnings.Count}):");
                foreach (var warning in Warnings)
                    sb.AppendLine($"  {warning}");
            }

            if (Stages.Count > 0)
            {
                sb.AppendLine("\nCompilation stages:");
                foreach (var stage in Stages)
                {
                    var duration = stage.EndTime.HasValue
                        ? (stage.EndTime.Value - stage.StartTime).TotalMilliseconds
                        : -1;
                    var durationStr = duration >= 0 ? $"{duration:F2}ms" : "incomplete";
                    var details = stage.Details != null ? $" ({stage.Details})" : "";
                    sb.AppendLine($"  {stage.Name}: {durationStr}{details}");
                }
            }

            return sb.ToString();
        }
    }

    public class CompilationStage
    {
        public string Name { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string Details { get; set; }
    }

    /// <summary>
    /// High-level facade for the entire grammar system.
    /// Provides a simple API for compiling and executing grammars.
    /// </summary>
    public class GrammarEngine
    {
        private GrammarIR _compiledGrammar;
        private OverrideManager _overrides;
        private int _defaultSeed;
        private IGrammarFileReader _fileReader;

        /// <summary>
        /// Creates a new GrammarEngine with optional file reader.
        /// If no file reader is provided, uses FileSystemGrammarReader by default.
        /// </summary>
        public GrammarEngine(IGrammarFileReader fileReader = null)
        {
            _overrides = new OverrideManager();
            _defaultSeed = 0;
            _fileReader = fileReader ?? new FileSystemGrammarReader();
        }

        /// <summary>
        /// Compiles grammar source text from a string.
        /// </summary>
        public bool CompileGrammar(string source, out string errorMessage)
        {
            var pipeline = new GrammarCompilerPipeline(source);
            var result = pipeline.Compile();

            if (result.Success)
            {
                _compiledGrammar = result.GrammarIR;
                errorMessage = null;
                return true;
            }

            var sb = new StringBuilder();
            foreach (var error in result.Errors)
                sb.AppendLine(error.ToString());
            errorMessage = sb.ToString();
            return false;
        }

        /// <summary>
        /// Compiles grammar from a .pgr file using the configured file reader.
        /// </summary>
        public bool CompileGrammarFromFile(string filePath, out string errorMessage)
        {
            try
            {
                if (!_fileReader.Exists(filePath))
                {
                    errorMessage = $"File not found: {filePath} (using {_fileReader.ReaderName} reader)";
                    return false;
                }

                string source = _fileReader.ReadAllText(filePath);
                return CompileGrammar(source, out errorMessage);
            }
            catch (Exception ex)
            {
                errorMessage = $"Error reading file '{filePath}': {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Sets the file reader to use for loading grammar files.
        /// Useful for switching between file system and Unity resource loading.
        /// </summary>
        public void SetFileReader(IGrammarFileReader fileReader)
        {
            _fileReader = fileReader ?? throw new ArgumentNullException(nameof(fileReader));
        }

        /// <summary>
        /// Generates a derivation tree from the compiled grammar.
        /// </summary>
        public DerivationTree Generate(Symbol startSymbol = null, int? seed = null)
        {
            if (_compiledGrammar == null)
                throw new InvalidOperationException("No grammar compiled. Call CompileGrammar first.");

            var actualSeed = seed ?? _defaultSeed;
            
            // Use entry symbol if no start symbol provided
            if (startSymbol == null)
            {
                startSymbol = new Symbol(_compiledGrammar.EntrySymbol);
            }

            var executor = new DerivationExecutor(_compiledGrammar, actualSeed);
            executor.SetOverrides(_overrides);

            return executor.Execute(startSymbol, actualSeed);
        }

        /// <summary>
        /// Sets the default seed for generation.
        /// </summary>
        public void SetSeed(int seed)
        {
            _defaultSeed = seed;
        }

        /// <summary>
        /// Gets the current override manager.
        /// </summary>
        public OverrideManager GetOverrides()
        {
            return _overrides;
        }

        /// <summary>
        /// Sets a new override manager.
        /// </summary>
        public void SetOverrides(OverrideManager overrides)
        {
            _overrides = overrides ?? new OverrideManager();
        }

        /// <summary>
        /// Gets the compiled grammar IR.
        /// </summary>
        public GrammarIR GetCompiledGrammar()
        {
            return _compiledGrammar;
        }

        /// <summary>
        /// Checks if a grammar is currently compiled.
        /// </summary>
        public bool IsGrammarCompiled()
        {
            return _compiledGrammar != null;
        }
    }
}
