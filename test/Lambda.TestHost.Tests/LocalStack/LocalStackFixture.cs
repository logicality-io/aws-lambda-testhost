using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Commands;
using Ductus.FluentDocker.Services;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Logicality.AWS.Lambda.TestHost.LocalStack
{
    public class LocalStackFixture : IAsyncDisposable
    {
        private readonly IContainerService _localStack;
        private readonly ITestOutputHelper _outputHelper;
        private const int ContainerPort = 4566;

        public LocalStackFixture(LambdaTestHost lambdaTestHost,
            IContainerService localStack,
            Uri serviceUrl,
            ITestOutputHelper outputHelper)
        {
            ServiceUrl = serviceUrl;
            LambdaTestHost = lambdaTestHost;
            _localStack = localStack;
            _outputHelper = outputHelper;

            AWSCredentials =  new BasicAWSCredentials("not", "used");
        }
        
        public Uri ServiceUrl { get; }

        public LambdaTestHost LambdaTestHost { get; }

        public AWSCredentials AWSCredentials { get; }

        public static async Task<LocalStackFixture> Create(
            ITestOutputHelper outputHelper,
            string services,
            bool setLambdaForwardUrl = false,
            bool setLambdaFallbackUrl = false)
        {
            // Runs a the Lambda TestHost (invoke api) on a random port
            var settings = new LambdaTestHostSettings(() => new TestLambdaContext())
            {
                ConfigureLogging = logging =>
                {
                    logging.AddXUnit(outputHelper);
                    logging.SetMinimumLevel(LogLevel.Debug);
                }
            };
            settings.AddFunction(new LambdaFunctionInfo(
                nameof(SimpleLambdaFunction),
                typeof(SimpleLambdaFunction),
                nameof(SimpleLambdaFunction.FunctionHandler)));
            var lambdaTestHost = await LambdaTestHost.Start(settings);

            var lambdaForwardUrl = new UriBuilder(lambdaTestHost.ServiceUrl)
            {
                Host = Environment.OSVersion.Platform == PlatformID.Win32NT
                    ? "host.docker.internal"
                    : "172.17.0.1"
            };

            var url = lambdaForwardUrl.ToString();
            url = url.Remove(url.Length - 1);

            var environment = new List<string>
            {
                $"SERVICES={services}",
                "LS_LOG=debug",
            };
            if (setLambdaFallbackUrl)
            {
                environment.Add($"LAMBDA_FALLBACK_URL={url}");
                outputHelper.WriteLine($"Using LAMBDA_FALLBACK_URL={url}");
            }

            if (setLambdaForwardUrl)
            {
                environment.Add($"LAMBDA_FORWARD_URL={url}");
                outputHelper.WriteLine($"Using LAMBDA_FORWARD_URL={url}");
            }

            var localStackBuilder = new Builder()
                .UseContainer()
                .WithName($"lambda-testhost-localstack-{Guid.NewGuid()}")
                .UseImage("localstack/localstack:latest")
                .WithEnvironment(environment.ToArray())
                .ExposePort(0, ContainerPort)
                .WaitForPort($"{ContainerPort}/tcp", 10000, "127.0.0.1");

            var localStack = localStackBuilder.Build().Start();

            var port = localStack
                .GetConfiguration()
                .NetworkSettings
                .Ports.First()
                .Value.First()
                .HostPort;

            var localstackServiceUrl = new Uri($"http://localhost:{port}");
            return new LocalStackFixture(lambdaTestHost, localStack, localstackServiceUrl, outputHelper);
        }

        public async ValueTask DisposeAsync()
        {
            await LambdaTestHost.DisposeAsync();

            var hosts = new Hosts().Discover();
            var docker = hosts.FirstOrDefault(x => x.IsNative) ?? hosts.FirstOrDefault(x => x.Name == "default");

            await Task.Delay(1000);
            _outputHelper.WriteLine("--- Begin container logs ---");
            using (var logs = docker?.Host.Logs(_localStack.Id, certificates: docker.Certificates))
            {
                var line = logs!.Read();
                while (line != null)
                {
                    _outputHelper.WriteLine(line);
                    line = logs!.Read();
                }
            }
            _outputHelper.WriteLine("--- End container logs ---");

            _localStack.RemoveOnDispose = true;
            _localStack.Dispose();
        }
    }
}