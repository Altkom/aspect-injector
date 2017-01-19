using AspectInjector.Broker;
using Mono.Cecil;
using System.Collections.Generic;

namespace AspectInjector.BuildTask.Models
{
    internal class InjectionStatement
    {
        public TypeDefinition AdviceClassType { get; set; }
    }
}