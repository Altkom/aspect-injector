using AspectInjector.Broker;
using AspectInjector.Core.Models;
using Mono.Cecil;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace AspectInjector.Core.Models
{
    internal class Assets
    {
        public List<AspectDefinition> Aspects { get; set; }
    }

    internal class AspectDefinition
    {
        public TypeDefinition Host { get; set; }
        public List<Effect> Effects { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Aspect.Scope Scope { get; set; }
    }

    public abstract class Effect
    {
    }
}

namespace AspectInjector.Core.Mixin
{
    internal class MixinEffect : Effect
    {
        public TypeReference InterfaceType { get; set; }
    }
}

namespace AspectInjector.Core.Advice
{
    internal class AdviceEffect : Effect
    {
        public List<AdviceEffectParameter> Params { get; internal set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Broker.Advice.Target Target { get; internal set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public Broker.Advice.Type Type { get; internal set; }

        public MethodDefinition Method { get; internal set; }
    }

    internal class AdviceEffectParameter
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public Broker.Advice.Argument.Source Source { get; internal set; }

        public int Index { get; internal set; }
    }
}