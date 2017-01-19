using AspectInjector.Broker;
using AspectInjector.Core.Models;
using Mono.Cecil;
using System.Collections.Generic;

namespace AspectInjector.Core.Models
{
    internal class AspectDefinition
    {
        public TypeDefinition Host { get; set; }
        public List<Effect> Effects { get; set; }
        public Aspect.Scope Scope { get; set; }
    }

    public abstract class Effect
    {
    }
}

namespace AspectInjector.Core.Mixin
{
    internal class Mixin : Effect
    {
        public TypeReference InterfaceType { get; set; }
    }
}

namespace AspectInjector.Core.Advice
{
    internal class Advice : Effect
    {
        public List<Broker.Advice.Argument.Source> Args { get; internal set; }
        public Broker.Advice.Target Target { get; internal set; }
        public Broker.Advice.Type Type { get; internal set; }
    }
}