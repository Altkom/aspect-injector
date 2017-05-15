using AspectInjector.Broker;
using AspectInjector.Samples.Logging.Aspects;
using System.Threading;
using System.Threading.Tasks;

namespace AspectInjector.Samples.Logging
{
    [Inject(typeof(LoggingAspect))]
    public class SampleService
    {
        public int GetCount()
        {
            Thread.Sleep(3000);

            return 10;
        }

        public async Task<int> GetCountAsync()
        {
            await Task.Delay(2000);

            return 10;
        }
    }
}
