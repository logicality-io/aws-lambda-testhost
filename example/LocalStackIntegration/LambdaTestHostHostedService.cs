using System;
using System.Threading;
using System.Threading.Tasks;
using Logicality.AWS.Lambda.TestHost;
using Microsoft.Extensions.Hosting;

namespace LocalStackIntegration
{
    public class LambdaTestHostHostedService : IHostedService
    {
        private readonly HostedServiceContext _context;
        private LambdaTestHost _lambdaTestHost;
        
        public Uri ServiceUrl { get; private set; }

        public LambdaTestHostHostedService(HostedServiceContext context)
        {
            _context = context;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var settings = new LambdaTestHostSettings(() => new TestLambdaContext())
            {
                WebHostUrl = "http://127.0.0.1:5004"
            };
            settings.AddFunction(new LambdaFunctionInfo(
                nameof(SimpleLambdaFunction),
                typeof(SimpleLambdaFunction),
                nameof(SimpleLambdaFunction.FunctionHandler)));
            _lambdaTestHost = await LambdaTestHost.Start(settings);

            ServiceUrl = _lambdaTestHost.ServiceUrl;

            _context.LambdaTestHost = this;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await _lambdaTestHost.DisposeAsync();
        }
    }
}