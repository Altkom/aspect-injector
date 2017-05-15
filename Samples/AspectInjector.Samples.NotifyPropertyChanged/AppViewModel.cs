using AspectInjector.Samples.NotifyPropertyChanged.Aspects;

namespace AspectInjector.Samples.NotifyPropertyChanged
{
    
    public class AppViewModel
    {
        [Notify(NotifyAlso = "FullName")]
        public string FirstName { get; set; }

        [Notify(NotifyAlso = "FullName")]
        public string LastName { get; set; }

        public string FullName
        {
            get
            {
                return FirstName + " " + LastName;
            }
        }
    }
}
