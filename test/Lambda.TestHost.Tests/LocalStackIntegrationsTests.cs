using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;
using Logicality.AWS.Lambda.TestHost.Functions;
using Xunit;
using Xunit.Abstractions;

namespace Logicality.AWS.Lambda.TestHost
{
    public class LocalStackIntegrationsTests : IAsyncLifetime
    {
        private readonly ITestOutputHelper _outputHelper;
        private LambdaTestHost _lambdaTestHost;
        private IContainerService _containerService;
        private Uri _stepFunctionsServiceUrl;
        private const int ContainerPort = 8083;
        public const int Port = 8083;

        public LocalStackIntegrationsTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task Foo()
        {
            // 1. Create the StepFunctions Client
            var credentials = new BasicAWSCredentials("not", "used");
           
        }

        public async Task InitializeAsync()
        {
            var settings = new LambdaTestHostSettings(() => new TestLambdaContext());
            settings.AddFunction(new LambdaFunctionInfo(
                nameof(SimpleLambdaFunction),
                typeof(SimpleLambdaFunction),
                nameof(SimpleLambdaFunction.FunctionHandler)));
            _lambdaTestHost = await LambdaTestHost.Start(settings);

            var dockerInternal = new UriBuilder(_lambdaTestHost.ServiceUrl)
            {
                Host = "host.docker.internal"
            };
            /*_containerService = new Builder()
                .UseContainer()
                .WithName("lambda-testhost-stepfunctions")
                .UseImage("amazon/aws-stepfunctions-local:latest")
                .WithEnvironment(
                    $"LAMBDA_ENDPOINT={dockerInternal.Uri}")
                .ReuseIfExists()
                .ExposePort(Port, ContainerPort)
                .WaitForPort($"{ContainerPort}/tcp", 10000, "127.0.0.1")
                .Build()
                .Start();*/

            _containerService = new Builder()
                .UseContainer()
                .WithName("lambda-testhost-localstack")
                .UseImage("localstack/localstack:latest")
                .WithEnvironment(
                    "SERVICES=sqs",
                    $"LAMBDA_FORWARD_URL={dockerInternal}")
                .ReuseIfExists()
                .ExposePort(Port, ContainerPort)
                .ExposePort(443, 443)
                .WaitForPort($"{ContainerPort}/tcp", 10000, "127.0.0.1")
                .Build();

            _stepFunctionsServiceUrl = new Uri($"http://localhost:{Port}");
        }

        public Task DisposeAsync()
        {
            _containerService.RemoveOnDispose = true;
            _containerService.Dispose();
            return Task.CompletedTask;
        }
    }
}
