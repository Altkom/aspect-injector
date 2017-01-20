using AspectInjector.Broker;
using AspectInjector.BuildTask.Common;
using AspectInjector.BuildTask.Contexts;
using AspectInjector.BuildTask.Extensions;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AspectInjector.BuildTask.Models;

namespace AspectInjector.BuildTask.Validation
{
    //TODO: Fire all compilation exceptions at once

    public static class Validator
    {
        private static List<ValidationRule> _rules = new List<ValidationRule>();

        static Validator()
        {
            _rules.Add(new ValidationRule() { Source = Advice.Argument.Source.Instance, ShouldBeOfType = typeof(object) });
            _rules.Add(new ValidationRule() { Source = Advice.Argument.Source.Target, ShouldBeOfType = typeof(Func<object[], object>) });
            _rules.Add(new ValidationRule() { Source = Advice.Argument.Source.Type, ShouldBeOfType = typeof(Type) });
            _rules.Add(new ValidationRule() { Source = Advice.Argument.Source.ReturnType, ShouldBeOfType = typeof(Type) });
            _rules.Add(new ValidationRule() { Source = Advice.Argument.Source.Method, ShouldBeOfType = typeof(MethodBase) });
            _rules.Add(new ValidationRule() { Source = Advice.Argument.Source.Arguments, ShouldBeOfType = typeof(object[]) });
            _rules.Add(new ValidationRule() { Source = Advice.Argument.Source.Name, ShouldBeOfType = typeof(string) });
            _rules.Add(new ValidationRule() { Source = Advice.Argument.Source.ReturnValue, ShouldBeOfType = typeof(object) });
        }

        public static void ValidateCustomAspectDefinition(CustomAttribute attribute)
        {
            attribute.AttributeType.Resolve().CustomAttributes.GetAttributeOfType<AttributeUsageAttribute>();
        }

        public static void ValidateAdviceMethodParameter(ParameterDefinition parameter, MethodReference adviceMethod)
        {
            var argumentAttribute = parameter.CustomAttributes.GetAttributeOfType<Advice.Argument>();
            if (argumentAttribute == null)
                throw new CompilationException("Unbound advice arguments are not supported", adviceMethod);

            var source = (Advice.Argument.Source)argumentAttribute.ConstructorArguments[0].Value;

            var rule = _rules.FirstOrDefault(r => r.Source == source);
            if (rule != null)
            {
                if (!parameter.ParameterType.IsTypeOf(rule.ShouldBeOfType))
                    throw new CompilationException("Argument should be of type " + rule.ShouldBeOfType.Namespace + "." + rule.ShouldBeOfType.Name + " to inject Advice.Argument.Source." + source.ToString(), adviceMethod);
            }
        }

        internal static void ValidateAspectDefinitions(IEnumerable<InjectionStatement> allAspectDefinitions, TypeReference refer)
        {
            if (refer is GenericInstanceType)
                throw new CompilationException($"Generic types as targets are not supported {refer.FullName}.", refer);

            if (refer.HasGenericParameters)
                throw new CompilationException($"Generic types as targets are not supported {refer.FullName}.", refer);

            foreach (var injections in allAspectDefinitions)
                if (!injections.AdviceClassType.CustomAttributes.HasAttributeOfType<Aspect>())
                    throw new CompilationException($"Cannot inject something which is not an aspect. Consider mark {injections.AdviceClassType.FullName} with [Aspect] attribute.", refer);
        }

        internal static void ValidateAdviceMethod(MethodDefinition adviceMethod)
        {
            if (adviceMethod.GenericParameters.Any() || adviceMethod.ReturnType.IsGenericParameter)
                throw new CompilationException("Advice cannot be generic", adviceMethod);
        }

        internal static void ValidateAdviceInjectionContext(Contexts.AdviceInjectionContext context, Advice.Target target)
        {
            //if (target == Advice.Target.Constructor)
            //{
            //    if (!context.AdviceMethod.ReturnType.IsTypeOf(typeof(void)))
            //        throw new CompilationException("Advice of Advice.Target.Constructor can be System.Void only", context.AdviceMethod);
            //}

            if (context.InjectionPoint == Advice.Type.After || context.InjectionPoint == Advice.Type.Before)
            {
                if (!context.AdviceMethod.ReturnType.IsTypeOf(typeof(void)))
                    throw new CompilationException("Advice of Advice.Type." + context.InjectionPoint.ToString() + " can be System.Void only", context.AdviceMethod);
            }

            if (context.InjectionPoint == Advice.Type.Around)
            {
                if ((target & Advice.Target.Constructor) == Advice.Target.Constructor)
                    throw new CompilationException("Advice of Advice.Type." + context.InjectionPoint.ToString() + " can't be applied to constructors", context.AdviceMethod);

                if (!context.AdviceMethod.ReturnType.IsTypeOf(typeof(object)))
                    throw new CompilationException("Advice of Advice.Type." + context.InjectionPoint.ToString() + " should return System.Object", context.AdviceMethod);
            }
        }

        internal static void ValidateAdviceClassType(TypeDefinition adviceClassType)
        {
            if (adviceClassType.GenericParameters.Any())
                throw new CompilationException("Advice class cannot be generic", adviceClassType);
        }

        internal static void ValidateAspectContexts(IEnumerable<AspectContext> contexts)
        {
            foreach (var context in contexts)
            {
                if (context.AdviceClassFactory == null)
                    throw new CompilationException("Cannot found empty constructor for aspect.", context.AdviceClassType);
            }
        }
    }
}