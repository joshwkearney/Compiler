﻿using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.Features.Functions;
using Attempt20.Parsing;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Attempt20.Features.Containers {
    public class MemberInvokeSyntaxA : ISyntaxA {
        private readonly string memberName;
        private readonly ISyntaxA target;
        private readonly IReadOnlyList<ISyntaxA> args;

        public TokenLocation Location { get; }

        public MemberInvokeSyntaxA(TokenLocation location, ISyntaxA target, string memberName, IReadOnlyList<ISyntaxA> args) {
            this.Location = location;
            this.target = target;
            this.memberName = memberName;
            this.args = args;
        }

        public ISyntaxB CheckNames(INameRecorder names) {
            var target = this.target.CheckNames(names);
            var args = this.args.Select(x => x.CheckNames(names)).ToArray();
            var region = IdentifierPath.HeapPath;

            if (names.CurrentRegion != IdentifierPath.StackPath) {
                region = names.CurrentRegion;
            }

            return new MemberInvokeSyntaxB(this.Location, target, this.memberName, args, region);
        }
    }

    public class MemberInvokeSyntaxB : ISyntaxB {
        private readonly IdentifierPath region;
        private readonly string memberName;
        private readonly ISyntaxB target;
        private readonly IReadOnlyList<ISyntaxB> args;

        public MemberInvokeSyntaxB(
            TokenLocation location,
            ISyntaxB target, 
            string memberName, 
            IReadOnlyList<ISyntaxB> args, 
            IdentifierPath region) {

            this.target = target;
            this.memberName = memberName;
            this.args = args;
            this.region = region;
            this.Location = location;
        }

        public TokenLocation Location { get; set; }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            var target = this.target.CheckTypes(types);
            var args = this.args.Select(x => x.CheckTypes(types)).Prepend(target).ToArray();

            // Make sure this method exists
            if (!types.TryGetMethodPath(target.ReturnType, this.memberName).TryGetValue(out var path)) {
                throw TypeCheckingErrors.MemberUndefined(this.Location, target.ReturnType, this.memberName);
            }

            // Get the function
            var func = types.TryGetFunction(path).GetValue();

            // Make sure the arg count lines up
            if (args.Length != func.Parameters.Count) {
                throw TypeCheckingErrors.ParameterCountMismatch(this.Location, func.Parameters.Count, args.Length);
            }

            // Make sure the arg types line up
            for (int i = 0; i < args.Length; i++) {
                var expected = func.Parameters[i].Type;
                var actual = args[i].ReturnType;

                if (expected != actual) {
                    throw TypeCheckingErrors.UnexpectedType(this.Location, expected, actual);
                }
            }

            var lifetimes = ImmutableHashSet.Create<IdentifierPath>();

            if (func.ReturnType.GetCopiability(types) == TypeCopiability.Conditional) {
                lifetimes = args
                    .Select(x => x.Lifetimes)
                    .Aggregate(ImmutableHashSet.Create<IdentifierPath>(), (x, y) => x.Union(y))
                    .Union(target.Lifetimes)
                    .Add(this.region)
                    .Remove(IdentifierPath.StackPath);
            }

            return new SingularFunctionInvokeSyntaxC(path, args, this.region.Segments.Last(), func.ReturnType, lifetimes);
        }
    }
}
