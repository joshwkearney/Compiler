﻿using System.Collections.Generic;
using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;

namespace Trophy.Features.FlowControl {
    public class WhileSyntaxA : ISyntaxA {
        private readonly ISyntaxA cond, body;

        public TokenLocation Location { get; set; }

        public WhileSyntaxA(TokenLocation location, ISyntaxA cond, ISyntaxA body) {
            this.Location = location;
            this.cond = cond;
            this.body = body;
        }

        public ISyntaxB CheckNames(INamesRecorder names) {
            var cond = this.cond.CheckNames(names);
            var body = this.body.CheckNames(names);

            return new WhileSyntaxB(this.Location, cond, body);
        }
    }

    public class WhileSyntaxB : ISyntaxB {
        private readonly ISyntaxB cond, body;

        public TokenLocation Location { get; }

        public IImmutableSet<VariableUsage> VariableUsage {
            get => this.cond.VariableUsage.Union(body.VariableUsage);
        }

        public WhileSyntaxB(TokenLocation loc, ISyntaxB cond, ISyntaxB body) {
            this.Location = loc;
            this.cond = cond;
            this.body = body;
        }

        public ISyntaxC CheckTypes(ITypesRecorder types) {
            var cond = this.cond.CheckTypes(types);
            var body = this.body.CheckTypes(types);

            // Make sure the condition is a boolean
            if (types.TryUnifyTo(cond, ITrophyType.Boolean).TryGetValue(out var newCond)) {
                cond = newCond;
            }
            else {
                throw TypeCheckingErrors.UnexpectedType(this.cond.Location, ITrophyType.Boolean, cond.ReturnType);
            }

            return new WhileSyntaxC(cond, body);
        }
    }

    public class WhileSyntaxC : ISyntaxC {
        private readonly ISyntaxC cond, body;

        public ITrophyType ReturnType => ITrophyType.Void;

        public ImmutableHashSet<IdentifierPath> Lifetimes => ImmutableHashSet.Create<IdentifierPath>();

        public WhileSyntaxC(ISyntaxC cond, ISyntaxC body) {
            this.cond = cond;
            this.body = body;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var loopBody = new List<CStatement>();
            var writer = new CStatementWriter();

            writer.StatementWritten += (s, e) => loopBody.Add(e);

            var cond = this.cond.GenerateCode(declWriter, writer);
            cond = CExpression.Not(cond);
            cond = CExpression.Invoke(CExpression.VariableLiteral("HEDLEY_UNLIKELY"), new[] { cond });

            loopBody.Add(CStatement.If(cond, new[] { CStatement.Break() }));
            loopBody.Add(CStatement.NewLine());

            var body = this.body.GenerateCode(declWriter, writer);

            statWriter.WriteStatement(CStatement.Comment("While loop"));
            statWriter.WriteStatement(CStatement.While(CExpression.IntLiteral(1), loopBody));
            statWriter.WriteStatement(CStatement.NewLine());

            return CExpression.IntLiteral(0);
        }
    }
}
