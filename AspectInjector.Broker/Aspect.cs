using System;

namespace AspectInjector.Broker
{
    /// <summary>
    /// Markrs specified class to be an Aspect and configures aspect usage scenarios.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class Aspect : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Aspect" /> class.
        /// </summary>
        /// <param name="scope">Scope in which aspect is instantiated.</param>
        public Aspect(Scope scope)
        {
        }

        /// <summary>
        /// Aspect creation scope enumeration.
        /// </summary>
        public enum Scope
        {
            /// <summary>
            /// Instantiate aspect globally as singleton.
            /// </summary>
            Global = 0,

            /// <summary>
            /// Instantiate aspect per instance.
            /// </summary>
            Instance = 1
        }
    }
}