﻿using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;

namespace Trophy.Features.Functions {
    public class FunctionAccessSyntaxA : ISyntaxA {
        private readonly IdentifierPath funcPath;

        public TokenLocation Location { get; }

        public FunctionAccessSyntaxA(TokenLocation loc, IdentifierPath funcPath) {
            this.Location = loc;
            this.funcPath = funcPath;
        }

        public ISyntaxB CheckNames(INamesRecorder names) {
            var region = names.Context.Region;

            return new FunctionAccessSyntaxBC(this.Location, this.funcPath, region);
        }

        public IOption<ITrophyType> ResolveToType(INamesRecorder names) {
            return Option.Some(new SingularFunctionType(this.funcPath));
        }
    }

    public class FunctionAccessSyntaxBC : ISyntaxB, ISyntaxC {
        private readonly IdentifierPath path, region;

        public TokenLocation Location { get; }

        public ITrophyType ReturnType => new SingularFunctionType(this.path);

        public ImmutableHashSet<IdentifierPath> Lifetimes => new[] { this.region }.ToImmutableHashSet();

        public IImmutableSet<VariableUsage> VariableUsage {
            get => ImmutableHashSet.Create<VariableUsage>();
        }

        public FunctionAccessSyntaxBC(TokenLocation loc, IdentifierPath funcPath, IdentifierPath region) {
            this.Location = loc;
            this.path = funcPath;
            this.region = region;
        }

        public ISyntaxC CheckTypes(ITypesRecorder types) {
            return this;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            return CExpression.IntLiteral(0);
        }
    }
}
