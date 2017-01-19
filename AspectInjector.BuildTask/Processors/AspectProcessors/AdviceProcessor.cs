using AspectInjector.Broker;
using AspectInjector.BuildTask.Contexts;
using AspectInjector.BuildTask.Contracts;
using AspectInjector.BuildTask.Extensions;
using AspectInjector.BuildTask.Validation;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace AspectInjector.BuildTask.Processors.AspectProcessors
{
    internal class AdviceProcessor : IAspectProcessor
    {
        private readonly IAspectInjector<AdviceInjectionContext> _injector;

        public AdviceProcessor(IAspectInjector<AdviceInjectionContext> injector)
        {
            _injector = injector;
        }

        public bool CanProcess(TypeDefinition aspectType)
        {
            return aspectType.Methods.Any(m => m.CustomAttributes.HasAttributeOfType<Advice>());
        }

        public void Process(AspectContext context)
        {
            var adviceContexts = GetAdviceMethods(context.AdviceClassType).SelectMany(m => ProcessAdvice(m, context)).Where(ac => ac != null).ToList();

            foreach (var adviceContext in adviceContexts)
                _injector.Inject(adviceContext);
        }

        private static bool CheckTarget(TargetMethodContext targetMethodContext, Advice.Target target)
        {
            var targetMethod = targetMethodContext.TargetMethod;

            if (targetMethod.IsAbstract ||
                targetMethod.IsPInvokeImpl)
            {
                return false;
            }

            if (targetMethod.IsConstructor)
            {
                return target == Advice.Target.Constructor;
            }

            if (targetMethod.IsGetter)
            {
                return target == Advice.Target.Getter;
            }

            if (targetMethod.IsSetter)
            {
                return target == Advice.Target.Setter;
            }

            if (targetMethod.IsAddOn)
            {
                return target == Advice.Target.EventAdd;
            }

            if (targetMethod.IsRemoveOn)
            {
                return target == Advice.Target.EventRemove;
            }

            if (!targetMethod.CustomAttributes.HasAttributeOfType<CompilerGeneratedAttribute>())
            {
                return target == Advice.Target.Method;
            }

            return false;
        }

        private static IEnumerable<MethodDefinition> GetAdviceMethods(TypeDefinition adviceClassType)
        {
            Validator.ValidateAdviceClassType(adviceClassType);

            return adviceClassType.Methods.Where(m => m.CustomAttributes.HasAttributeOfType<Advice>());
        }

        private static IEnumerable<Advice.Argument.Source> GetAdviceArgumentsSources(MethodDefinition adviceMethod)
        {
            foreach (var parameter in adviceMethod.Parameters)
            {
                Validator.ValidateAdviceMethodParameter(parameter, adviceMethod);

                var argumentAttribute = parameter.CustomAttributes.GetAttributeOfType<Advice.Argument>();
                var source = (Advice.Argument.Source)argumentAttribute.ConstructorArguments[0].Value;
                yield return source;
            }
        }

        private static IEnumerable<AdviceInjectionContext> ProcessAdvice(MethodDefinition adviceMethod,
            AspectContext parentContext)
        {
            Validator.ValidateAdviceMethod(adviceMethod);

            var adviceAttribute = adviceMethod.CustomAttributes.GetAttributeOfType<Advice>();

            var points = (Advice.Type)adviceAttribute.ConstructorArguments[0].Value;
            var targets = (Advice.Target)adviceAttribute.ConstructorArguments[1].Value;

            foreach (Advice.Type point in Enum.GetValues(typeof(Advice.Type)).Cast<Advice.Type>().Where(p => (points & p) != 0))
            {
                foreach (Advice.Target target in Enum.GetValues(typeof(Advice.Target)).Cast<Advice.Target>().Where(t => (targets & t) != 0))
                {
                    if (CheckTarget(parentContext.TargetMethodContext, target))
                    {
                        var context = new AdviceInjectionContext() { AspectContext = parentContext };

                        context.AdviceMethod = adviceMethod;
                        context.AdviceArgumentsSources = GetAdviceArgumentsSources(adviceMethod).ToList();
                        context.InjectionPoint = point;

                        Validator.ValidateAdviceInjectionContext(context, target);

                        yield return context;
                    }
                }
            }
        }
    }
}