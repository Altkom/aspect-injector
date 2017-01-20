using AspectInjector.BuildTask.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using AspectInjector.BuildTask.Models.Converters;
using AspectInjector.BuildTask.Models;
using AspectInjector.Core.Models;
using AspectInjector.BuildTask.Extensions;
using AspectInjector.Broker;
using AspectInjector.Core.Advice;

namespace AspectInjector.BuildTask.Processors.ModuleProcessors
{
    internal class MetadataWriterProcessor : IModuleProcessor
    {
        internal const string _aspectCacheResourceName = "__a$_assets";

        public readonly List<string> _loadedMetadata = new List<string>();

        public void ProcessModule(ModuleDefinition module)
        {
            var aspects = module.Types.SelectMany(ReadAspects).ToList();

            var assets = new Assets
            {
                Aspects = aspects
            };

            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                MissingMemberHandling = MissingMemberHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
            };

            serializerSettings.Converters.Add(new TypeReferenceConverter(module));
            serializerSettings.Converters.Add(new TypeDefinitionConverter(module));
            serializerSettings.Converters.Add(new MethodDefinitionConverter(module));

            var resource = module.Resources.FirstOrDefault(r => r.ResourceType == ResourceType.Embedded && r.Name == _aspectCacheResourceName);

            if (resource != null)
                module.Resources.Remove(resource);

            var json = JsonConvert.SerializeObject(assets, serializerSettings);

            resource = new EmbeddedResource(_aspectCacheResourceName, ManifestResourceAttributes.Private, Encoding.UTF8.GetBytes(json));

            module.Resources.Add(resource);
        }

        private List<AspectDefinition> ReadAspects(TypeDefinition type)
        {
            var aspects = new List<AspectDefinition>();

            foreach (var nestedType in type.NestedTypes)
                aspects.AddRange(ReadAspects(nestedType));

            if (type.CustomAttributes.HasAttributeOfType<Aspect>())
            {
                var aspectAttribute = type.CustomAttributes.GetAttributeOfType<Aspect>();

                aspects.Add(new AspectDefinition
                {
                    Host = type,
                    Scope = (Aspect.Scope)aspectAttribute.ConstructorArguments[0].Value,
                    Effects = ReadEffects(type)
                });
            }

            return aspects;
        }

        private List<Effect> ReadEffects(TypeDefinition type)
        {
            var effects = new List<Effect>();

            if (type.CustomAttributes.HasAttributeOfType<Mixin>())
            {
                var aspectAttribute = type.CustomAttributes.GetAttributeOfType<Mixin>();

                effects.Add(new AspectInjector.Core.Mixin.MixinEffect
                {
                    InterfaceType = (TypeReference)aspectAttribute.ConstructorArguments[0].Value
                });
            }

            foreach (var method in type.Methods)
            {
                if (method.CustomAttributes.HasAttributeOfType<Advice>())
                {
                    var aspectAttribute = method.CustomAttributes.GetAttributeOfType<Advice>();

                    effects.Add(new AspectInjector.Core.Advice.AdviceEffect
                    {
                        Type = (Advice.Type)aspectAttribute.ConstructorArguments[0].Value,
                        Target = (Advice.Target)aspectAttribute.ConstructorArguments[1].Value,
                        Params = ReadAdviceArgs(method),
                        Method = method
                    });
                }
            }

            return effects;
        }

        private List<AdviceEffectParameter> ReadAdviceArgs(MethodDefinition method)
        {
            var paramss = method.Parameters.Select(p => new AdviceEffectParameter { Source = (Advice.Argument.Source)p.CustomAttributes.Single(ca => ca.IsAttributeOfType<Advice.Argument>()).ConstructorArguments[0].Value, Index = p.Index }).ToList();

            return paramss;
        }
    }
}