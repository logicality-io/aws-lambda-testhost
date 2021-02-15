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

            var credentials = new BasicAWSCredentials("not", "used");
            var sqsClient = new AmazonSQSClient(credentials, new AmazonSQSConfig
            {
                ServiceURL = context.LocalStack.ServiceUrl.ToString()
            });
            var lambdaClient = new AmazonLambdaClient(credentials, new AmazonLambdaConfig
            {
                ServiceURL = context.LocalStack.ServiceUrl.ToString()
            });
            var createQueueRequest = new CreateQueueRequest("test-q");
            var createQueueResponse = await sqsClient.CreateQueueAsync(createQueueRequest);

            var createFunctionRequest = new CreateFunctionRequest
            {
                FunctionName = nameof(SimpleLambdaFunction),
                Runtime = "netcoreapp3.1",
                Handler = nameof(SimpleLambdaFunction.FunctionHandler),
                Role = "arn:aws:iam::000000000000:role/foo",
                Code = new FunctionCode()
                {
                    S3Bucket = "foo",
                    S3Key = "bar",
                },
            };

            var createFunctionResponse = await lambdaClient.CreateFunctionAsync(createFunctionRequest);

            var createEventSourceMappingRequest = new CreateEventSourceMappingRequest
            {
                EventSourceArn = $"arn:aws:sqs:eus-east-1:000000000000:{createQueueRequest.QueueName}",
                FunctionName = nameof(SimpleLambdaFunction),
            };
            
            var createEventSourceMappingResponse = await lambdaClient.CreateEventSourceMappingAsync(createEventSourceMappingRequest);

            var sendMessageRequest = new SendMessageRequest(createQueueResponse.QueueUrl, "message");
            await sqsClient.SendMessageAsync(sendMessageRequest);
            
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
                            .Host<LambdaTestHostHostedService>()
                            .Host<LocalStackHostedService>());
                })
                .UseSerilog(logger);

            return (hostBuilder, context);
        }
    }
}
