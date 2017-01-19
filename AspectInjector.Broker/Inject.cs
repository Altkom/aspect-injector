using System;

namespace AspectInjector.Broker
{
    /// <summary>
    /// Marks member to be injection target for specific Aspect.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Event, AllowMultiple = true)]
    public sealed class Inject : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Inject" /> class.
        /// </summary>
        /// <param name="aspect">Aspect to inject.</param>
        public Inject(Type aspectType)
        {
        }
    }
}