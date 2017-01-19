using AspectInjector.Broker;
using AspectInjector.BuildTask.Contracts;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AspectInjector.BuildTask.Contexts
{
    public class AdviceInjectionContext : IInjectionContext
    {
        public List<Advice.Argument.Source> AdviceArgumentsSources { get; set; }

        public MethodDefinition AdviceMethod { get; set; }

        public Advice.Type InjectionPoint { get; set; }

        public AspectContext AspectContext { get; set; }
    }
}