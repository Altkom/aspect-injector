using AspectInjector.Broker;
using AspectInjector.BuildTask.Common;
using AspectInjector.BuildTask.Contexts;
using AspectInjector.BuildTask.Contracts;
using AspectInjector.BuildTask.Extensions;
using AspectInjector.BuildTask.Models;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AspectInjector.BuildTask.Injectors
{
    internal class AdviceInjector : IAspectInjector<AdviceInjectionContext>
    {
        public void Inject(AdviceInjectionContext context)
        {
            FieldReference aspectInstanceField = context.AspectContext.TargetTypeContext.GetOrCreateAspectReference(context.AspectContext);

            PointCut injectionPoint;

            switch (context.InjectionPoint)
            {
                case Advice.Type.Before:
                    injectionPoint = context.AspectContext.TargetMethodContext.EntryPoint;
                    break;

                case Advice.Type.After:
                    injectionPoint = context.AspectContext.TargetMethodContext.ReturnPoint;
                    break;

                case Advice.Type.Around:
                    injectionPoint = context.AspectContext.TargetMethodContext.CreateNewAroundPoint();
                    break;

                default: throw new NotSupportedException(context.InjectionPoint.ToString() + " is not supported (yet?)");
            }

            var argumentValue = ResolveArgumentsValues(
                        context.AspectContext,
                        context.AdviceArgumentsSources)
                        .ToArray();

            if (!aspectInstanceField.Resolve().IsStatic) injectionPoint.LoadSelfOntoStack();
            injectionPoint.LoadField(aspectInstanceField);
            injectionPoint.InjectMethodCall(context.AdviceMethod, argumentValue);
        }

        protected IEnumerable<object> ResolveArgumentsValues(
           AspectContext context,
           List<Advice.Argument.Source> sources)
        {
            foreach (var argumentSource in sources)
            {
                switch (argumentSource)
                {
                    case Advice.Argument.Source.Instance:
                        yield return context.TargetMethodContext.TargetMethod.IsStatic ? Markers.DefaultMarker : Markers.InstanceSelfMarker;
                        break;

                    case Advice.Argument.Source.Type:
                        yield return context.TargetTypeContext.TypeDefinition;
                        break;

                    case Advice.Argument.Source.Method:
                        yield return context.TargetMethodContext.TopWrapper ?? context.TargetMethodContext.TargetMethod;
                        break;

                    case Advice.Argument.Source.Arguments:
                        yield return Markers.AllArgsMarker;
                        break;

                    case Advice.Argument.Source.Name:
                        yield return context.TargetName;
                        break;

                    case Advice.Argument.Source.ReturnType:
                        yield return context.TargetMethodContext.TargetMethod.ReturnType;
                        break;

                    case Advice.Argument.Source.ReturnValue:
                        yield return context.TargetMethodContext.MethodResultVariable ?? Markers.DefaultMarker;
                        break;

                    case Advice.Argument.Source.Target:
                        yield return Markers.TargetFuncMarker;
                        break;

                    default:
                        throw new NotSupportedException(argumentSource.ToString() + " is not supported (yet?)");
                }
            }
        }
    }
}