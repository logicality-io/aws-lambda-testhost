using System;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;
using Logicality.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LocalStackIntegration
{
    public class StepFunctionsHostedService : DockerHostedService
    {
        private readonly HostedServiceContext _context;
        public const int Port = 8083;
        private const int ContainerPort = 8083;

        public StepFunctionsHostedService(
            HostedServiceContext context, 
            ILogger<DockerHostedService> logger,
            bool leaveRunning = false) 
            : base(logger, leaveRunning)
        {
            _context = context;
        }

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

        protected override string ContainerName => "lambda-testhost-stepfunctions";
    }
}