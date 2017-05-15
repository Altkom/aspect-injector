using AspectInjector.Broker;
using System;
using System.ComponentModel;

namespace AspectInjector.Samples.NotifyPropertyChanged.Aspects
{
    [CustomAspectDefinition(typeof(NotifyPropertyChangedAspect))]
    class NotifyAttribute : Attribute
    {
        public string NotifyAlso { get; set; }
    }


    [Mixin(typeof(INotifyPropertyChanged))]
    class NotifyPropertyChangedAspect : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = (s, e) => { };

        [Advice(Advice.Type.After, Advice.Target.Setter)]
        public void AfterSetter(
            [Advice.Argument(Advice.Argument.Source.Instance)] object source,
            [Advice.Argument(Advice.Argument.Source.Name)] string propName,
            [Advice.Argument(Advice.Argument.Source.)] object data)
        {
            PropertyChanged(source, new PropertyChangedEventArgs(propName));

            var additionalPropName = (data as NotifyAttribute).NotifyAlso;

            if (!string.IsNullOrEmpty(additionalPropName))
            {
                PropertyChanged(source, new PropertyChangedEventArgs(additionalPropName));
            }
        }
    }
}
