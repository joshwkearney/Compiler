﻿using System;
using Attempt17.Features.Containers.Composites;
using Attempt17.Parsing;

namespace Attempt17.Features.Containers {
    public class ContainersFeature : ILanguageFeature {
        private readonly CompositeFeature structsFeature = new CompositeFeature();

        private readonly ContainersTypeChecker typeChecker = new ContainersTypeChecker();

        public void RegisterSyntax(ISyntaxRegistry registry) {
            this.structsFeature.RegisterSyntax(registry);

            registry.RegisterParseTree<MemberUsageParseSyntax>(this.typeChecker.CheckMemberUsage);
            registry.RegisterParseTree<NewSyntax>(this.typeChecker.CheckNew);
        }
    }
}