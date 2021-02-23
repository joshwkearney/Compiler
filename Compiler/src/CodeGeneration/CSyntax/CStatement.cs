﻿using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt20.CodeGeneration.CSyntax {
    public abstract class CStatement {
        public static CStatement VariableDeclaration(CType type, string name, CExpression assign) {
            return new CVariableDeclaration(type, name, Option.Some(assign));
        }

        public static CStatement VariableDeclaration(CType type, string name) {
            return new CVariableDeclaration(type, name, Option.None<CExpression>());
        }

        public static CStatement Return(CExpression expr) {
            return new CReturnStatement(expr);
        }

        public static CStatement If(CExpression cond, IReadOnlyList<CStatement> affirm, IReadOnlyList<CStatement> neg) {
            return new CIfStatement(cond, affirm, neg);
        }

        public static CStatement If(CExpression cond, IReadOnlyList<CStatement> affirm) {
            return If(cond, affirm, new CStatement[] { });
        }

        public static CStatement Assignment(CExpression left, CExpression right) {
            return new CAssignment(left, right);
        }

        public static CStatement FromExpression(CExpression expr) {
            return new ExpressionStatement() {
                Expression = expr
            };
        }

        public static CStatement ArrayDeclaration(CType elementType, string name, CExpression size) {
            return new CArrayDeclaration() {
                ArraySize = size,
                ArrayType = elementType,
                Name = name
            };
        }

        public static CStatement While(CExpression cond, IReadOnlyList<CStatement> body) {
            return new WhileStatement() {
                Conditon = cond,
                Body = body
            };
        }

        public static CStatement Break() {
            return new CBreakStatement();
        }

        public static CStatement NewLine() {
            return new CNewLine();
        }

        private CStatement() { }

        public abstract void WriteToC(int indentLevel, StringBuilder sb);

        private class CBreakStatement : CStatement {
            public override void WriteToC(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.AppendLine("break;");
            }
        }

        private class WhileStatement : CStatement {
            public CExpression Conditon { get; set; }

            public IReadOnlyList<CStatement> Body { get; set; }

            public override void WriteToC(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.Append("while (").Append(this.Conditon).Append(") {");

                if (this.Body.Any()) {
                    sb.AppendLine();
                }

                foreach (var stat in this.Body) {
                    stat.WriteToC(indentLevel + 1, sb);
                }

                CHelper.Indent(indentLevel, sb);
                sb.AppendLine("}");
            }
        }

        private class ExpressionStatement : CStatement {
            public CExpression Expression { get; set; }

            public override void WriteToC(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.Append(this.Expression).AppendLine(";");
            }
        }

        private class CAssignment : CStatement {
            private readonly CExpression left;
            private readonly CExpression right;

            public CAssignment(CExpression left, CExpression right) {
                this.left = left;
                this.right = right;
            }

            public override void WriteToC(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.Append(this.left).Append(" = ").Append(this.right).AppendLine(";");
            }
        }

        private class CArrayDeclaration : CStatement {
            public CExpression ArraySize { get; set; }

            public CType ArrayType { get; set; }

            public string Name { get; set; }

            public override void WriteToC(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.Append(this.ArrayType).Append(" ").Append(this.Name).Append("[").Append(this.ArraySize).AppendLine("];");
            }
        }

        private class CVariableDeclaration : CStatement {
            private readonly CType varType;
            private readonly string name;
            private readonly IOption<CExpression> assignExpr;

            public CVariableDeclaration(CType type, string name, IOption<CExpression> assign) {
                this.varType = type;
                this.name = name;
                this.assignExpr = assign;
            }

            public override void WriteToC(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.Append(this.varType.ToString()).Append(" ").Append(this.name);

                if (this.assignExpr.TryGetValue(out var assign)) {
                    sb.Append(" = ").Append(assign.ToString());
                }

                sb.AppendLine(";");
            }
        }

        private class CReturnStatement : CStatement {
            private readonly CExpression value;

            public CReturnStatement(CExpression value) {
                this.value = value;
            }

            public override void WriteToC(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.Append("return ").Append(this.value.ToString()).AppendLine(";");
            }
        }

        private class CIfStatement : CStatement {
            private readonly CExpression cond;
            private readonly IReadOnlyList<CStatement> affirmBranch;
            private readonly IReadOnlyList<CStatement> negBranch;

            public CIfStatement(CExpression cond, IReadOnlyList<CStatement> affirm, IReadOnlyList<CStatement> neg) {
                this.cond = cond;
                this.affirmBranch = affirm;
                this.negBranch = neg;
            }

            public override void WriteToC(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                sb.Append("if (").Append(this.cond.ToString()).Append(") { ");

                if (this.affirmBranch.Any()) {
                    sb.AppendLine();

                    foreach (var stat in this.affirmBranch) {
                        stat.WriteToC(indentLevel + 1, sb);
                    }
                }

                CHelper.Indent(indentLevel, sb);
                sb.AppendLine("} ");

                if (this.negBranch.Any()) {
                    CHelper.Indent(indentLevel, sb);
                    sb.AppendLine("else {");

                    foreach (var stat in this.negBranch) {
                        stat.WriteToC(indentLevel + 1, sb);
                    }

                    CHelper.Indent(indentLevel, sb);
                    sb.AppendLine("}");
                }
            }
        }

        private class CNewLine : CStatement {
            public override void WriteToC(int indentLevel, StringBuilder sb) {
                sb.AppendLine();
            }
        }
    }
}