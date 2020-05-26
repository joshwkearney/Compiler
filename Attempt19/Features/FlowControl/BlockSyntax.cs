﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Attempt19.TypeChecking;
using Attempt19;
using Attempt19.CodeGeneration;
using Attempt19.Features.FlowControl;
using Attempt19.Parsing;
using Attempt19.Types;

namespace Attempt19 {
    public static partial class SyntaxFactory {
        public static Syntax MakeBlock(IReadOnlyList<Syntax> stats, TokenLocation loc) {
            return new Syntax() {
                Data = SyntaxData.From(new BlockData() {
                    Statements = stats,
                    Location = loc }),
                Operator = SyntaxOp.FromNameDeclarator(BlockTransformations.DeclareNames)
            };
        }
    }
}

namespace Attempt19.Features.FlowControl {
    public class BlockData : IParsedData, ITypeCheckedData, IFlownData {
        public IReadOnlyList<Syntax> Statements { get; set; }

        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<VariableCapture> EscapingVariables { get; set; }

        public IdentifierPath BlockPath { get; set; }
    }

    public static class BlockTransformations {
        private static int blockId = 0;
        private static int blockReturnCounter = 0;

        public static Syntax DeclareNames(IParsedData data, IdentifierPath scope, NameCache names) {
            var block = (BlockData)data;

            // Set the block path
            block.BlockPath = scope.Append("block" + blockId++);

            // Delegate name declarations
            block.Statements = block.Statements
                .Select(x => x.DeclareNames(block.BlockPath, names))
                .ToArray();

            return new Syntax() {
                Data = SyntaxData.From(block),
                Operator = SyntaxOp.FromNameResolver(ResolveNames)
            };
        }

        public static Syntax ResolveNames(IParsedData data, NameCache names) {
            var block = (BlockData)data;

            names.PushLocalFrame();

            // Delegate name resolutions
            block.Statements = block.Statements
                .Select(x => x.ResolveNames(names))
                .ToArray();

            names.PopLocalFrame();

            return new Syntax() {
                Data = SyntaxData.From(block),
                Operator = SyntaxOp.FromTypeDeclarator(DeclareTypes)
            };
        }

        public static Syntax DeclareTypes(IParsedData data, TypeCache types) {
            var block = (BlockData)data;

            // Delegate type declarations
            block.Statements = block.Statements
                .Select(x => x.DeclareTypes(types))
                .ToArray();

            return new Syntax() {
                Data = SyntaxData.From(block),
                Operator = SyntaxOp.FromTypeResolver(ResolveTypes)
            };
        }

        public static Syntax ResolveTypes(IParsedData data, TypeCache types, ITypeUnifier unifier) {
            var block = (BlockData)data;

            // Delegate type resolutions
            block.Statements = block.Statements
                .Select(x => x.ResolveTypes(types, unifier))
                .ToArray();

            // Set the return type
            block.ReturnType = block.Statements
                .LastOrNone()
                .Select(x => x.Data.AsTypeCheckedData().GetValue().ReturnType)
                .GetValueOr(() => VoidType.Instance);

            // Set escaping variables
            if (block.Statements.Any()) {
                var last = block.Statements.Last().Data.AsTypeCheckedData().GetValue();

                block.EscapingVariables = last.EscapingVariables
                    .SelectMany(x => types.FlowGraph.FindAllCapturedVariables(x.VariablePath))
                    .Concat(last.EscapingVariables)
                    .ToImmutableHashSet();
            }
            else {
                block.EscapingVariables = ImmutableHashSet.Create<VariableCapture>();
            }

            // Set variable lifetimes
            foreach (var cap in block.EscapingVariables) {
                types.VariableLifetimes[cap.VariablePath] = block.BlockPath.Pop();
            }

            var incorrectEscape = block.EscapingVariables
                .Where(x => x.Kind != VariableCaptureKind.MoveCapture)
                .Where(x => x.VariablePath.StartsWith(block.BlockPath))
                .ToArray();

            // Make sure that no variables improperly escape
            if (incorrectEscape.Any()) {
                var loc = block.Statements.Last().Data.AsParsedData().Location;
                var var = incorrectEscape.First().VariablePath;

                throw TypeCheckingErrors.VariableScopeExceeded(loc, var);
            }

            return new Syntax() {
                Data = SyntaxData.From(block),
                Operator = SyntaxOp.FromFlowAnalyzer(AnalyzeFlow)
            };
        }

        public static Syntax AnalyzeFlow(ITypeCheckedData data, TypeCache types, FlowCache flows) {
            var block = (BlockData)data;

            // Delegate flow analysis
            block.Statements = block.Statements
                .Select(x => x.AnalyzeFlow(types, flows))
                .ToArray();

            return new Syntax() {
                Data = SyntaxData.From(block),
                Operator = SyntaxOp.FromCodeGenerator(GenerateCode)
            };
        }

        public static CBlock GenerateCode(IFlownData data, ICScope scope, ICodeGenerator gen) {
            // Get a new scope
            scope = new BlockCScope(scope);

            var block = (BlockData)data;
            var stats = block.Statements
                .Select(x => x.Data.AsFlownData().GetValue())
                .ToArray();

            // Generate statements
            var genStats = block.Statements
                .Select(x => x.GenerateCode(scope, gen))
                .ToArray();

            var writer = new CWriter();

            if (!genStats.Any()) {
                return new CBlock("0");
            }

            var returnType = gen.Generate(block.ReturnType);
            var returnVal = genStats.Last().Value;
            var tempName = $"$block_return_" + blockReturnCounter++;

            var varsToCleanUp = scope
                .GetUndestructedVariables()
                .ToImmutableDictionary(x => x.Key, x => x.Value)
                .AddRange(
                    stats
                        .Zip(genStats, (x, y) => (v: y.Value, t: x.ReturnType))
                        .Select(x => new KeyValuePair<string, LanguageType>(x.v, x.t))
                        .SkipLast(1)
                );

            var lines = genStats
                .Select(x => x.SourceLines)
                .Aggregate((x, y) => x.AddRange(y));

            writer.Lines(lines);
            writer.Line("// Block cleanup");
            writer.VariableInit(returnType, tempName, returnVal);
            writer.Lines(ScopeHelper.CleanupScope(varsToCleanUp, gen));
            writer.EmptyLine();

            return writer.ToBlock(tempName);
        }
    }
}