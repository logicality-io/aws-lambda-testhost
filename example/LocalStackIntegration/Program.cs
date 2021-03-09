using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Logicality.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using HostBuilder = Microsoft.Extensions.Hosting.HostBuilder;

namespace LocalStackIntegration
{
    public class Program
    {
        private static async Task Main(string[] args)
        {
            var (hostBuilder, context) = CreateHostBuilder(args);
            var host = hostBuilder.Build();
            await host.StartAsync();

            var sqsClient = new AmazonSQSClient(context.LocalStack.AWSCredentials, new AmazonSQSConfig
            {
                ServiceURL = context.LocalStack.ServiceUrl.ToString()
            });
            var lambdaClient = new AmazonLambdaClient(context.LocalStack.AWSCredentials, new AmazonLambdaConfig
            {
                ServiceURL = context.LocalStack.ServiceUrl.ToString()
            });

            var invokeRequest = new InvokeRequest
            {
                FunctionName = "simple",
                Payload = "{}",
            };

            var invokeResponse = await lambdaClient.InvokeAsync(invokeRequest);

            /*var sendMessageRequest = new SendMessageRequest(context.LocalStack.QueueUrl, "message");
            await sqsClient.SendMessageAsync(sendMessageRequest);*/
            
            await host.WaitForShutdownAsync();
        }

        private static (IHostBuilder, HostedServiceContext) CreateHostBuilder(string[] args)
        {
            var loggerConfiguration = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Error)
                .Enrich.FromLogContext()
                .WriteTo.Logger(l =>
                {
                    l.WriteHostedServiceMessagesToConsole();
                });

            var logger = loggerConfiguration.CreateLogger();

            var context = new HostedServiceContext();

            var hostBuilder = new HostBuilder()
                .UseConsoleLifetime()
                .ConfigureAppConfiguration(builder =>
                {
                    builder
                        .AddCommandLine(args)
                        .AddUserSecrets<Program>();
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton(context);
                    services.AddTransient<LocalStackHostedService>();

                    services
                        .AddSequentialHostedServices("root", r => r
                            .Host<LambdaTestHostHostedService>());
                    //.Host<LocalStackHostedService>());
                })
                .UseSerilog(logger);

            return (hostBuilder, context);
        }
    }
}
