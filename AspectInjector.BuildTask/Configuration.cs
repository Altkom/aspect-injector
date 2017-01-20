using AspectInjector.BuildTask.Contracts;
using AspectInjector.BuildTask.Injectors;
using AspectInjector.BuildTask.Processors.AspectProcessors;
using AspectInjector.BuildTask.Processors.ModuleProcessors;
using System.Collections.Generic;

namespace AspectInjector.BuildTask
{
    public class Configuration
    {
        private static List<IModuleProcessor> _processorsTree;

        public static List<IModuleProcessor> GetProcessorsTree()
        {
            if (_processorsTree == null)
            {
                _processorsTree = new List<IModuleProcessor>
                {
                    new MetadataReaderProcessor(),

                    new InjectionProcessor(new List<IAspectProcessor>
                    {
                        new AdviceProcessor(new AdviceInjector()),
                        new InterfaceProcessor(new InterfaceInjector())
                    }),

                    new MetadataWriterProcessor(),

                    new Janitor()
                };
            }

            return _processorsTree;
        }
    }
}