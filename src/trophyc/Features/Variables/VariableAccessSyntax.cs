﻿using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;

namespace Trophy.Features.Variables {
    public enum VariableAccessKind {
        ValueAccess, LiteralAccess
    }

    public class VariableAccessSyntaxB : ISyntaxB {
        private readonly IdentifierPath path;
        private readonly VariableAccessKind kind;

        public TokenLocation Location { get; }

        public IImmutableSet<VariableUsage> VariableUsage {
            get => new[] { new VariableUsage(this.path, VariableUsageKind.Captured) }.ToImmutableHashSet();
        }

        public VariableAccessSyntaxB(TokenLocation loc, IdentifierPath path, VariableAccessKind kind) {
            this.Location = loc;
            this.path = path;
            this.kind = kind;
        }

        public ISyntaxC CheckTypes(ITypesRecorder types) {
            var info = types.TryGetVariable(this.path).GetValue();
            var returnType = info.Type;
            var lifetimes = ImmutableHashSet.Create<IdentifierPath>();

            if (this.kind == VariableAccessKind.ValueAccess) {
                lifetimes = info.ValueLifetimes;

                // If the variable type is copiable, don't propagate any lifetimes
                if (returnType.GetCopiability(types) == TypeCopiability.Unconditional) {
                    lifetimes = ImmutableHashSet.Create<IdentifierPath>();
                }
            }
            else {
                lifetimes = info.VariableLifetimes;

                // Make sure we're not literally accessing a non-variable parameter
                if (info.DefinitionKind == VariableDefinitionKind.Parameter) {
                    throw TypeCheckingErrors.ExpectedVariableType(this.Location, info.Type);
                }

                // For non-parameter access, return a variable type of the accessed variable
                if (info.DefinitionKind == VariableDefinitionKind.LocalVar || info.DefinitionKind == VariableDefinitionKind.ParameterVar) {
                    returnType = new VarRefType(returnType, false);
                }
                else {
                    returnType = new VarRefType(returnType, true);
                }
            }

            return new VariableAccessdSyntaxC(info, this.kind, returnType, lifetimes);
        }
    }

    public class VariableAccessdSyntaxC : ISyntaxC {
        private readonly VariableInfo info;
        private readonly VariableAccessKind kind;

        public ITrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; }

        public ImmutableDictionary<IdentifierPath, VariableUsageKind> VariableUsage { get; }

        public VariableAccessdSyntaxC(VariableInfo info, VariableAccessKind kind, ITrophyType type, ImmutableHashSet<IdentifierPath> lifetimes) {
            this.info = info;
            this.kind = kind;
            this.ReturnType = type;
            this.Lifetimes = lifetimes;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var cname = "$" + this.info.Name + this.info.UniqueId;

            if (this.kind == VariableAccessKind.ValueAccess) {
                if (this.info.DefinitionKind == VariableDefinitionKind.ParameterRef || this.info.DefinitionKind == VariableDefinitionKind.ParameterVar) {
                    return CExpression.Dereference(CExpression.VariableLiteral(cname));
                }
                else {
                    return CExpression.VariableLiteral(cname);
                }
            }
            else {
                if (this.info.DefinitionKind == VariableDefinitionKind.ParameterRef || this.info.DefinitionKind == VariableDefinitionKind.ParameterVar) {
                    return CExpression.VariableLiteral(cname);
                }
                else {
                    return CExpression.AddressOf(CExpression.VariableLiteral(cname));
                }
            }
        }
    }
}
