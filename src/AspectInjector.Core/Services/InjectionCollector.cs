﻿using AspectInjector.Core.Contracts;
using AspectInjector.Core.Extensions;
using AspectInjector.Core.Models;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;
using System;

namespace AspectInjector.Core.Services
{
    public class InjectionCollector : IInjectionCollector
    {
        private readonly IAssetsCache _cache;
        private readonly ILogger _log;

        public InjectionCollector(IAssetsCache cache, ILogger logger)
        {
            _cache = cache;
            _log = logger;
        }

        public IReadOnlyCollection<Injection> Collect(AssemblyDefinition assembly)
        {
            var aspects = ExtractInjections(assembly);

            foreach (var module in assembly.Modules)
            {
                aspects = aspects.Concat(ExtractInjections(module));

                foreach (var type in module.GetTypes())
                {
                    aspects = aspects.Concat(ExtractInjectionsFromClass(type));

                    aspects = aspects.Concat(type.Events.SelectMany(ExtractInjections));
                    aspects = aspects.Concat(type.Properties.SelectMany(ExtractInjections));
                    //aspects = aspects.Concat(type.Fields.SelectMany(ExtractInjections));
                    aspects = aspects.Concat(type.Methods.Where(m => m.IsNormalMethod() || m.IsConstructor).SelectMany(ExtractInjections));
                }
            }

            aspects = aspects.GroupBy(a => a).Select(g => g.Aggregate(MergeInjections)).ToList();

            return aspects.ToList();
        }

        protected virtual IEnumerable<Injection> ExtractInjections(ICustomAttributeProvider target)
        {
            var injections = Enumerable.Empty<Injection>();

            foreach (var attr in target.CustomAttributes.Where(a => a.AttributeType.IsTypeOf(typeof(Broker.Inject))).ToList())
            {
                injections = injections.Concat(ParseInjectionAttribute(target, attr));
                target.CustomAttributes.Remove(attr);
            }

            return injections;
        }


        protected virtual IEnumerable<Injection> ExtractInjectionsFromClass(TypeDefinition target)
        {
            var injections = ExtractInjections(target);

            var inheritedAttributes = GetBaseTypes(target.BaseType as TypeDefinition).SelectMany(p => p.CustomAttributes);

            inheritedAttributes = inheritedAttributes.Where(a => 
                a.AttributeType.IsTypeOf(typeof(Broker.Inject)) && 
                a.GetType().CustomAttributes.OfType<AttributeUsageAttribute>()
                .Any(au => au.Inherited)
            );

            foreach (var attr in inheritedAttributes.ToList())
            {
                injections = injections.Concat(ParseInjectionAttribute(target, attr));
                target.CustomAttributes.Remove(attr);
            }

            return injections;
        }

        private IEnumerable<TypeDefinition> GetBaseTypes(TypeDefinition target)
        {
            if (target == null) yield break;
            yield return target;

            if (target.BaseType != null)
            {
                foreach (var item in GetBaseTypes(target.BaseType as TypeDefinition))
                {
                    yield return item;
                }
            }
        }



        private IEnumerable<Injection> ParseInjectionAttribute(ICustomAttributeProvider target, CustomAttribute attr)
        {
            var aspectRef = attr.GetConstructorValue<TypeReference>(0);
            var aspect = _cache.ReadAspect(aspectRef.Resolve());

            if (aspect == null)
            {
                _log.LogError(CompilationMessage.From($"Type {aspectRef.FullName} should be an aspect class.", target));
                return Enumerable.Empty<Injection>();
            }

            var priority = attr.GetPropertyValue<Broker.Inject, ushort>(i => i.Priority);

            // var childFilter = attr.GetPropertyValue<Broker.Inject, InjectionChildFilter>(i => i.Filter);

            var injections = FindApplicableMembers(target, aspect, priority/*, childFilter*/);

            return injections;
        }

        private IEnumerable<Injection> FindApplicableMembers(ICustomAttributeProvider target, AspectDefinition aspect, ushort priority)
        {
            var result = Enumerable.Empty<Injection>();

            var assm = target as AssemblyDefinition;
            if (assm != null)
                result = result.Concat(assm.Modules.SelectMany(nt => FindApplicableMembers(nt, aspect, priority)));

            var module = target as ModuleDefinition;
            if (module != null)
                result = result.Concat(module.Types.SelectMany(nt => FindApplicableMembers(nt, aspect, priority)));

            var member = target as IMemberDefinition;
            if (member != null)
                result = result.Concat(CreateInjections(member, aspect, priority));

            var type = target as TypeDefinition;
            if (type != null)
            {
                result = result.Concat(type.Methods.Where(m => m.IsNormalMethod() || m.IsConstructor)
                    .SelectMany(m => FindApplicableMembers(m, aspect, priority)));
                result = result.Concat(type.Events.SelectMany(m => FindApplicableMembers(m, aspect, priority)));
                result = result.Concat(type.Properties.SelectMany(m => FindApplicableMembers(m, aspect, priority)));
                result = result.Concat(type.NestedTypes.SelectMany(nt => FindApplicableMembers(nt, aspect, priority)));
            }

            return result;
        }

        private IEnumerable<Injection> CreateInjections(IMemberDefinition target, AspectDefinition aspect, ushort priority)
        {
            return aspect.Effects.Where(e => e.IsApplicableFor(target)).Select(e => new Injection()
            {
                Target = target,
                Source = aspect,
                Priority = priority,
                Effect = e
            });
        }

        private Injection MergeInjections(Injection a1, Injection a2)
        {
            a1.Priority = Enumerable.Max(new[] { a1.Priority, a2.Priority });
            return a1;
        }

        //private static bool CheckFilter(MethodDefinition targetMethod,
        //    string targetName,
        //    ChildrenFilter aspectDefinition)
        //{
        //    var result = true;

        //    var nameFilter = aspectDefinition.NameFilter;
        //    var accessModifierFilter = aspectDefinition.AccessModifierFilter;

        //    if (!string.IsNullOrEmpty(nameFilter))
        //    {
        //        result = Regex.IsMatch(targetName, nameFilter);
        //    }

        //    if (result && accessModifierFilter != AccessModifier.Any)
        //    {
        //        if (targetMethod.IsPrivate)
        //        {
        //            result = (accessModifierFilter & AccessModifier.Private) != 0;
        //        }
        //        else if (targetMethod.IsFamily)
        //        {
        //            result = (accessModifierFilter & AccessModifier.Protected) != 0;
        //        }
        //        else if (targetMethod.IsAssembly)
        //        {
        //            result = (accessModifierFilter & AccessModifier.Internal) != 0;
        //        }
        //        else if (targetMethod.IsFamilyOrAssembly)
        //        {
        //            result = (accessModifierFilter & AccessModifier.ProtectedInternal) != 0;
        //        }
        //        else if (targetMethod.IsPublic)
        //        {
        //            result = (accessModifierFilter & AccessModifier.Public) != 0;
        //        }
        //    }

        //    return result;
        //}
    }
}