using AspectInjector.BuildTask.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace AspectInjector.BuildTask.Processors.ModuleProcessors
{
    internal class MetadataReaderProcessor : IModuleProcessor
    {
        private readonly string _aspectCache = "__a$_aspects";

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
            throw new NotImplementedException();
        }
    }
}