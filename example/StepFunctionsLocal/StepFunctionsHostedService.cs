using System;
using System.Threading;
using System.Threading.Tasks;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;
using Logicality.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace StepFunctionsLocal
{
    public class StepFunctionsHostedService : DockerHostedService
    {
        public const int Port = 8083;
        private readonly HostedServiceContext _context;
        private const int ContainerPort = 8083;

        public StepFunctionsHostedService(
            HostedServiceContext context, 
            ILogger<DockerHostedService> logger,
            bool leaveRunning = false) 
            : base(logger, leaveRunning)
        {
            _context = context;
        }

        public Uri ServiceUrl { get; } = new Uri($"http://localhost:{Port}");

        protected override IContainerService CreateContainerService()
        {
            var dockerInternal = new UriBuilder(_context.LambdaTestHost.ServiceUrl)
            {
                Host = "host.docker.internal"
            };
            return new Builder()
                .UseContainer()
                .WithName(ContainerName)
                .UseImage("amazon/aws-stepfunctions-local:latest")
                .WithEnvironment(
                    $"LAMBDA_ENDPOINT={dockerInternal.Uri}")
                .ReuseIfExists()
                .ExposePort(Port, ContainerPort)
                .WaitForPort($"{ContainerPort}/tcp", 10000, "127.0.0.1")
                .Build();
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await base.StartAsync(cancellationToken);
            _context.StepFunctions = this;
        }

        protected override string ContainerName => "lambda-testhost-stepfunctions";
    }
}