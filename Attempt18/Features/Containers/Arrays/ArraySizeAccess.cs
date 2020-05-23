﻿using System;
using System.Collections.Generic;
using Attempt18.Evaluation;
using Attempt18.Types;

namespace Attempt18.Features.Containers.Arrays {
    public class ArraySizeAccess : ISyntax {
        public IdentifierPath Scope { get; set; }

        public LanguageType ReturnType { get; set; }

        public IdentifierPath[] CapturedVariables { get; set; }

        public ISyntax Target { get; set; }

        public void AnalyzeFlow(TypeChache types, IFlowCache flow) {
            this.Target.AnalyzeFlow(types, flow);

            // This always returns an int, so there are no captured variables
            this.CapturedVariables = new IdentifierPath[0];
        }

        public void DeclareNames(NameCache<NameTarget> names) {
            throw new InvalidOperationException();
        }

        public void DeclareTypes(TypeChache  cache) {
            throw new InvalidOperationException();
        }

        public IEvaluateResult Evaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) {
            var target = (IEvaluateResult[])this.Target.Evaluate(memory).Value;

            return new AtomicEvaluateResult((long)target.Length);
        }

        public void PreEvaluate(Dictionary<IdentifierPath, IEvaluateResult> memory) {
            this.Target.PreEvaluate(memory);
        }

        public void ResolveNames(NameCache<NameTarget> names) {
            throw new InvalidOperationException();
        }

        public void ResolveScope(IdentifierPath containingScope) {
            throw new InvalidOperationException();
        }

        public ISyntax ResolveTypes(TypeChache  types) {
            throw new InvalidOperationException();
        }
    }
}
