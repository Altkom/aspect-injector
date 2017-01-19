﻿using AspectInjector.Broker;
using AspectInjector.BuildTask.Common;
using AspectInjector.BuildTask.Contexts;
using AspectInjector.BuildTask.Contracts;
using AspectInjector.BuildTask.Extensions;
using Mono.Cecil;
using System.Linq;

namespace AspectInjector.BuildTask.Processors.AspectProcessors
{
    public class InterfaceProcessor : IAspectProcessor
    {
        private readonly IAspectInjector<InterfaceInjectionContext> _injector;

        public InterfaceProcessor(IAspectInjector<InterfaceInjectionContext> injector)
        {
            _injector = injector;
        }

        public bool CanProcess(TypeDefinition aspectType)
        {
            return aspectType.CustomAttributes.HasAttributeOfType<Mixin>();
        }

        public void Process(AspectContext context)
        {
            var interfaceInjectionDefinitions = from ca in context.AdviceClassType.CustomAttributes
                                                where ca.IsAttributeOfType<Mixin>()
                                                select new { @interface = (TypeReference)ca.ConstructorArguments[0].Value };

            foreach (var interfaceInjectionDefinition in interfaceInjectionDefinitions)
            {
                var interfaceReference = interfaceInjectionDefinition.@interface;

                //todo:: process other InterfaceProxyInjectionAttribute parameters

                var interfaceInjectionContext = new InterfaceInjectionContext
                {
                    AspectContext = context,
                    InterfaceDefinition = interfaceInjectionDefinition.@interface.Resolve()
                };

                FillinInterfaceMembers(interfaceInjectionContext);

                //some validation

                _injector.Inject(interfaceInjectionContext);
            }
        }

        protected virtual void FillinInterfaceMembers(InterfaceInjectionContext context)
        {
            var aspectDefinition = context.AspectContext.AdviceClassType;
            var interfaceDefinition = context.InterfaceDefinition;
            var classDefinition = context.AspectContext.TargetTypeContext;

            if (!context.InterfaceDefinition.IsInterface)
                throw new CompilationException(context.InterfaceDefinition.Name + " is not an interface on interface injection definition on acpect " + aspectDefinition.Name, aspectDefinition);

            if (!context.AspectContext.AdviceClassType.ImplementsInterface(context.InterfaceDefinition))
                throw new CompilationException(aspectDefinition.Name + " should implement " + interfaceDefinition.Name, aspectDefinition);

            if (!classDefinition.TypeDefinition.ImplementsInterface(interfaceDefinition))
            {
                var ifaces = interfaceDefinition.GetInterfacesTree();

                foreach (var iface in ifaces)
                    classDefinition.TypeDefinition.Interfaces.Add(classDefinition.TypeDefinition.Module.Import(iface));
            }
            else if (!classDefinition.TypeDefinition.Interfaces.Any(i => i.IsTypeOf(interfaceDefinition)))
            {
                //In order to behave the same as csc
                classDefinition.TypeDefinition.Interfaces.Add(classDefinition.TypeDefinition.Module.Import(interfaceDefinition));
            }

            context.Methods = interfaceDefinition.GetInterfaceTreeMembers(td => td.Methods)
                .Where(m => !m.IsAddOn && !m.IsRemoveOn && !m.IsSetter && !m.IsGetter)
                .ToArray();

            context.Properties = interfaceDefinition.GetInterfaceTreeMembers(td => td.Properties)
                .ToArray();

            context.Events = interfaceDefinition.GetInterfaceTreeMembers(td => td.Events)
                .ToArray();
        }
    }
}