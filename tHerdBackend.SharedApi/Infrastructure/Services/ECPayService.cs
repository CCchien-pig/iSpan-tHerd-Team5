using Microsoft.Extensions.Options;
using tHerdBackend.SharedApi.Infrastructure.Config;

namespace tHerdBackend.SharedApi.Infrastructure.Services
{
    public class ECPayService
    {
        private readonly ECPaySettings _settings;

        public ECPayService(IOptions<ECPaySettings> options)
        {
            _settings = options.Value;
        }

        public void Test()
        {
            Console.WriteLine(_settings.MerchantID); // just to prove it works
        }
    }

}
