using System;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Logicality.Extensions.Hosting;
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
            var host = CreateHostBuilder(args).Build();
            await host.StartAsync();

            var credentials = new BasicAWSCredentials("not", "used");
            var config = new AmazonStepFunctionsConfig
            {
                ServiceURL = "http://localhost:8083"
            };
            var client = new AmazonStepFunctionsClient(credentials, config);

            var request = new CreateStateMachineRequest
            {
                Name = "Foo",
                Type = StateMachineType.STANDARD,
                Definition = @"{
  ""Comment"": ""A Hello World example demonstrating various state types of the Amazon States Language"",
  ""StartAt"": ""Invoke Lambda function"",
  ""States"": {
    ""Invoke Lambda function"": {
      ""Type"": ""Task"",
      ""Resource"": ""arn:aws:states:::lambda:invoke"",
      ""Parameters"": {
        ""FunctionName"": ""arn:aws:lambda:us-east-1:123456789012:function:simple:$LATEST"",
        ""Payload"": {
          ""Input.$"": ""$.Comment""
        }
      },
      ""End"": true
    }
  }
}"
            };

            var createStateMachineResponse = await client.CreateStateMachineAsync(request);

            var startExecutionRequest = new StartExecutionRequest
            {
                Name = Guid.NewGuid().ToString(),
                StateMachineArn = createStateMachineResponse.StateMachineArn,
                Input = @"{
    ""Comment"": ""Insert your JSON here""
}"
            };
            var startExecutionResponse = await client.StartExecutionAsync(startExecutionRequest);

            var getExecutionHistoryRequest = new GetExecutionHistoryRequest
            {
                ExecutionArn = startExecutionResponse.ExecutionArn,
                IncludeExecutionData = true,
            };
            var getExecutionHistoryResponse = await client.GetExecutionHistoryAsync(getExecutionHistoryRequest);

            await host.WaitForShutdownAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
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

            return new HostBuilder()
                .UseConsoleLifetime()
                .ConfigureServices(services =>
                {
                    services.AddSingleton(context);
                    services.AddTransient<LocalStackHostedService>();

                    services
                        .AddSequentialHostedServices("root", r => r
                            .Host<LambdaTestHostHostedService>());
                    //.Host<StepFunctionsHostedService>());
                })
                .UseSerilog(logger);
        }
    }
}
