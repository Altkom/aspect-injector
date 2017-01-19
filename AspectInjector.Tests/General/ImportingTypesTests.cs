using AspectInjector.Broker;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.ComponentModel;

namespace AspectInjector.Tests.General
{
    [TestClass]
    public class ImportingTypesTests
    {
    }

    [Inject(typeof(NotifyPropertyChangedAspect))]
    public class AppViewModel
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string FullName
        {
            get
            {
                return FirstName + " " + LastName;
            }
        }
    }

    [Aspect(Aspect.Scope.Instance)]
    [Mixin(typeof(INotifyPropertyChanged))]
    internal class NotifyPropertyChangedAspect : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = (s, e) => { };

        [Advice(Advice.Type.After, Advice.Target.Setter)]
        public void AfterSetter(
            [Advice.Argument(Advice.Argument.Source.Instance)] object source,
            [Advice.Argument(Advice.Argument.Source.Name)] string propName
            )
        {
            PropertyChanged(source, new PropertyChangedEventArgs(propName));
        }
    }
}