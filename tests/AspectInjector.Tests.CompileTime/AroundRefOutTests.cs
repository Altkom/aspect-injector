using AspectInjector.Broker;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;

namespace AspectInjector.CompileTimeTests
{
    [TestClass]
    public class AroundRefOutTests : CompileTimeTestRunner
    {
        [TestMethod]
        public void Can_Pack_And_Unpack_Ref_And_Out_Into_Array()
        {
            PE_Integrity_Is_Ok();

            var c = new TestClass();

            c.MyProperty = 1;
        }
        [Inject(typeof(TestAspectImplementation))]
        public class TestClass : BaseTestClass
        {
            [Inject(typeof(TestAspectImplementation))]
            public object Do2(object obj, ref object objRef, out object objOut, int value, ref int valueRef, out int valueOut, ref long longRef, ref double doubleRef, ref char charRef)
            {
                objOut = new object();
                valueOut = 1;

                return new object();
            }

            [Inject(typeof(TestAspectImplementation))]
            public static object Do1(object obj, ref object objRef, out object objOut, int value, ref int valueRef, out int valueOut, ref long longRef, ref double doubleRef, ref char charRef)
            {
                objOut = new object();
                valueOut = 1;

                return new object();
            }

            public int MyProperty1 { get; set; }
        }
        [Inject(typeof(TestAspectImplementation))]
        public class BaseTestClass
        {
            public int MyProperty { get; set; }
        }

        [Aspect(Aspect.Scope.Global)]
        public class TestAspectImplementation
        {
            [Advice(Advice.Type.Around, Advice.Target.Method)]
            public object AroundMethod([Advice.Argument(Advice.Argument.Source.Target)] Func<object[], object> target,
                [Advice.Argument(Advice.Argument.Source.Arguments)] object[] arguments)
            {
                return new object();
            }

            [Advice(Advice.Type.Before, Advice.Target.Setter)]
            public void Before(
    [Advice.Argument(Advice.Argument.Source.Arguments)] object[] args,
    //[Advice.Argument(Advice.Argument.Source.Attributes)] Attribute[] attrs,
    [Advice.Argument(Advice.Argument.Source.Instance)] object _this,
    [Advice.Argument(Advice.Argument.Source.Method)] MethodBase method,
    [Advice.Argument(Advice.Argument.Source.Name)] string name,
    [Advice.Argument(Advice.Argument.Source.ReturnType)] Type retType,
    [Advice.Argument(Advice.Argument.Source.ReturnValue)] object retVal,
    [Advice.Argument(Advice.Argument.Source.Target)] Func<object[], object> target,
    [Advice.Argument(Advice.Argument.Source.Type)] Type hostType
    )
            {

            }
        }
    }
}