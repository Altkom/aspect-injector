using AspectInjector.Broker;
using AspectInjector.BuildTask.Contexts;
using AspectInjector.BuildTask.Contracts;
using AspectInjector.BuildTask.Extensions;
using AspectInjector.BuildTask.Models;
using AspectInjector.BuildTask.Validation;
using Mono.Cecil;
using Mono.Collections.Generic;
using System.Collections.Generic;
using System.Linq;

namespace AspectInjector.BuildTask.Processors.ModuleProcessors
{
    internal class InjectionProcessor : IModuleProcessor
    {
        private readonly IEnumerable<IAspectProcessor> _processors;

        public InjectionProcessor(IEnumerable<IAspectProcessor> processors)
        {
            _processors = processors;
        }

        public void ProcessModule(ModuleDefinition module)
        {
            foreach (var @class in module.Types.Where(t => t.IsClass).SelectMany(t => t.GetClassesTree()))
            {
                var classAspectDefinitions = FindAspectDefinitions(@class.CustomAttributes);

                Validator.ValidateAspectDefinitions(classAspectDefinitions, @class);

                foreach (var method in @class.Methods.Where(m => !m.IsSetter && !m.IsGetter && !m.IsAddOn && !m.IsRemoveOn).ToList())
                {
                    var methodAspectDefinitions = FindAspectDefinitions(method.CustomAttributes);

                    Validator.ValidateAspectDefinitions(methodAspectDefinitions, @class);

                    ProcessAspectDefinitions(method, method.Name, classAspectDefinitions.Concat(methodAspectDefinitions));
                }

                foreach (var property in @class.Properties.ToList())
                {
                    var propertyAspectDefinitions = FindAspectDefinitions(property.CustomAttributes);
                    var allAspectDefinitions = classAspectDefinitions.Concat(propertyAspectDefinitions);

                    Validator.ValidateAspectDefinitions(allAspectDefinitions, @class);

                    if (property.GetMethod != null)
                    {
                        ProcessAspectDefinitions(property.GetMethod, property.Name, allAspectDefinitions);
                    }

                    if (property.SetMethod != null)
                    {
                        ProcessAspectDefinitions(property.SetMethod, property.Name, allAspectDefinitions);
                    }
                }

                foreach (var @event in @class.Events.ToList())
                {
                    var eventAspectDefinitions = FindAspectDefinitions(@event.CustomAttributes);
                    var allAspectDefinitions = classAspectDefinitions.Concat(eventAspectDefinitions);

                    Validator.ValidateAspectDefinitions(allAspectDefinitions, @class);

                    if (@event.AddMethod != null)
                    {
                        ProcessAspectDefinitions(@event.AddMethod, @event.Name, allAspectDefinitions);
                    }

                    if (@event.RemoveMethod != null)
                    {
                        ProcessAspectDefinitions(@event.RemoveMethod, @event.Name, allAspectDefinitions);
                    }
                }
            }

            //ValidateContexts(contexts);
        }

        private static bool CheckFilter(MethodDefinition targetMethod,
            string targetName,
            InjectionStatement aspectDefinition)
        {
            var result = true;
            return result;
        }

        private List<InjectionStatement> FindAspectDefinitions(Collection<CustomAttribute> collection)
        {
            var result = collection.GetAttributesOfType<Inject>().Select(ParseAspectAttribute).ToList();
            return result;
        }

        private InjectionStatement ParseAspectAttribute(CustomAttribute attr)
        {
            return new InjectionStatement()
            {
                AdviceClassType = ((TypeReference)attr.ConstructorArguments[0].Value).Resolve()
            };
        }

        private IEnumerable<InjectionStatement> MergeAspectDefinitions(IEnumerable<InjectionStatement> definitions)
        {
            var result = new List<InjectionStatement>();

            foreach (var cd in definitions)
            {
                var match = result.FirstOrDefault(ad => ad.AdviceClassType.Equals(cd.AdviceClassType));
                if (match == null)
                    result.Add(cd);
            }

            return result;
        }

        private void ProcessAspectDefinitions(MethodDefinition targetMethod,
            string targetName,
            IEnumerable<InjectionStatement> aspectDefinitions)
        {
            var filteredDefinitions = aspectDefinitions.Where(def => CheckFilter(targetMethod, targetName, def));
            var mergedDefinitions = MergeAspectDefinitions(filteredDefinitions);

            var contexts = mergedDefinitions
                .Select(def =>
                {
                    var adviceClassType = def.AdviceClassType;

                    var aspectScope = targetMethod.IsStatic ? Aspect.Scope.Global : Aspect.Scope.Instance;
                    if (adviceClassType.CustomAttributes.HasAttributeOfType<Aspect>())
                        aspectScope = (Aspect.Scope)adviceClassType.CustomAttributes.GetAttributeOfType<Aspect>().ConstructorArguments[0].Value;

                    var aspectContext = new AspectContext()
                    {
                        TargetName = targetName,
                        TargetTypeContext = TypeContextFactory.GetOrCreateContext(targetMethod.DeclaringType),
                        AdviceClassType = adviceClassType,
                        AdviceClassScope = aspectScope
                    };

                    return aspectContext;
                })
                .Where(ctx => _processors.Any(p => p.CanProcess(ctx.AdviceClassType)))
                .ToList();

            Validator.ValidateAspectContexts(contexts);

            foreach (var context in contexts)
            {
                var targetMethodContext = MethodContextFactory.GetOrCreateContext(targetMethod);
                context.TargetMethodContext = targetMethodContext; //setting it here for better performance

                foreach (var processor in _processors)
                    if (processor.CanProcess(context.AdviceClassType))
                        processor.Process(context);
            }
        }
    }
}