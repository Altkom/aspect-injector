using AspectInjector.Broker;
using AspectInjector.BuildTask.Extensions;
using Mono.Cecil;
using System.Linq;

namespace AspectInjector.BuildTask.Contexts
{
    public class AspectContext
    {
        private MethodDefinition _adviceClassFactory = null;

        public AspectContext()
        {
        }

        public MethodDefinition AdviceClassFactory
        {
            get
            {
                if (_adviceClassFactory == null)
                {
                    _adviceClassFactory = AdviceClassType.Methods
                    .Where(c => c.IsConstructor && !c.IsStatic && !c.Parameters.Any())
                    .FirstOrDefault();
                }

                return _adviceClassFactory;
            }
        }

        public TypeDefinition AdviceClassType { get; set; }

        public Aspect.Scope AdviceClassScope { get; set; }

        public TargetMethodContext TargetMethodContext { get; set; }

        public string TargetName { get; set; }

        public TargetTypeContext TargetTypeContext { get; set; }
    }
}