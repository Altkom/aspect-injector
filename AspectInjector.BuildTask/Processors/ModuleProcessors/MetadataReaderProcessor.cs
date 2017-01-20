using AspectInjector.BuildTask.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using AspectInjector.BuildTask.Models.Converters;
using AspectInjector.Core.Models;
using AspectInjector.Broker;
using AspectInjector.Core.Mixin;
using AspectInjector.Core.Advice;

namespace AspectInjector.BuildTask.Processors.ModuleProcessors
{
    internal class MetadataReaderProcessor : IModuleProcessor
    {
        public readonly List<string> _loadedMetadata = new List<string>();

        public void ProcessModule(ModuleDefinition module)
        {
            foreach (var reference in module.AssemblyReferences)
            {
                if (_loadedMetadata.Contains(reference.FullName))
                    continue;

                LoadMetadata(module.AssemblyResolver.Resolve(reference));

                _loadedMetadata.Add(reference.FullName);
            }
        }

        private void LoadMetadata(AssemblyDefinition assemblyDefinition)
        {
            foreach (var module in assemblyDefinition.Modules)
            {
                var assetsResource = module.Resources.FirstOrDefault(r => r.Name == MetadataWriterProcessor._aspectCacheResourceName && r.ResourceType == ResourceType.Embedded && r.IsPrivate);

                if (assetsResource != null)
                {
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

                    var assestsdata = Encoding.UTF8.GetString(((EmbeddedResource)assetsResource).GetResourceData());

                    var assets = JsonConvert.DeserializeObject<Assets>(assestsdata, serializerSettings);

                    ApplyAspects(module, assets.Aspects);
                }
            }
        }

        private void ApplyAspects(ModuleDefinition module, List<AspectDefinition> aspects)
        {
            var aspectAttrCtor = module.Import(typeof(Aspect)).Resolve().Methods.First(m => m.IsConstructor && m.Parameters.Count == 1);
            var mixinAttrCtor = module.Import(typeof(Mixin)).Resolve().Methods.First(m => m.IsConstructor && m.Parameters.Count == 1);
            var adviceAttrCtor = module.Import(typeof(Advice)).Resolve().Methods.First(m => m.IsConstructor && m.Parameters.Count == 2);
            var adviceArgAttrCtor = module.Import(typeof(Advice.Argument)).Resolve().Methods.First(m => m.IsConstructor && m.Parameters.Count == 1);

            foreach (var aspect in aspects)
            {
                var aspectAttr = new CustomAttribute(aspectAttrCtor);
                aspectAttr.ConstructorArguments.Add(new CustomAttributeArgument(module.Import(typeof(Aspect.Scope)), aspect.Scope));

                aspect.Host.CustomAttributes.Add(aspectAttr);

                foreach (var mixin in aspect.Effects.OfType<MixinEffect>())
                {
                    var mixinAttr = new CustomAttribute(mixinAttrCtor);
                    mixinAttr.ConstructorArguments.Add(new CustomAttributeArgument(module.Import(typeof(Type)), mixin.InterfaceType));

                    aspect.Host.CustomAttributes.Add(mixinAttr);
                }

                foreach (var advice in aspect.Effects.OfType<AdviceEffect>())
                {
                    var adviceAttr = new CustomAttribute(adviceAttrCtor);
                    adviceAttr.ConstructorArguments.Add(new CustomAttributeArgument(module.Import(typeof(Advice.Type)), advice.Type));
                    adviceAttr.ConstructorArguments.Add(new CustomAttributeArgument(module.Import(typeof(Advice.Target)), advice.Target));

                    advice.Method.CustomAttributes.Add(adviceAttr);

                    foreach (var par in advice.Params)
                    {
                        var argAttr = new CustomAttribute(adviceArgAttrCtor);
                        argAttr.ConstructorArguments.Add(new CustomAttributeArgument(module.Import(typeof(Advice.Argument.Source)), par.Source));

                        advice.Method.Parameters.First(p => p.Index == par.Index).CustomAttributes.Add(argAttr);
                    }
                }
            }
        }
    }
}