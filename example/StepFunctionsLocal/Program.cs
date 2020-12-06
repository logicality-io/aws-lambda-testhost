using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Logicality.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace StepFunctionsLocal
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var (hostBuilder, context) = CreateHostBuilder(args);
            var host = hostBuilder.Build();
            await host.StartAsync();

            // 1. Create the StepFunctions Client
            var credentials = new BasicAWSCredentials("not", "used");
            var config = new AmazonStepFunctionsConfig
            {
                ServiceURL = context.StepFunctions.ServiceUrl.ToString()
            };
            var client = new AmazonStepFunctionsClient(credentials, config);

            // 2. Create step machine with a single task the invokes a lambda function
            // The FUnctionName contains the lambda to be invoked.
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
        ""FunctionName"": ""arn:aws:lambda:us-east-1:123456789012:function:SimpleLambdaFunction:$LATEST"",
        ""Payload"": {
          ""Input.$"": ""$.Payload""
        }
      },
      ""End"": true
    }
  }
}"
            };

            var createStateMachineResponse = await client.CreateStateMachineAsync(request);

            // 3. Create a StepFunction execution.
            var startExecutionRequest = new StartExecutionRequest
            {
                Name = Guid.NewGuid().ToString(),
                StateMachineArn = createStateMachineResponse.StateMachineArn,
                Input = @"{
""Payload"": { 
  ""Foo"": ""Bar"" 
  }
}"
            };
            var startExecutionResponse = await client.StartExecutionAsync(startExecutionRequest);

            var getExecutionHistoryRequest = new GetExecutionHistoryRequest
            {
                ExecutionArn = startExecutionResponse.ExecutionArn,
                IncludeExecutionData = true,
            };


            while (true) //HACK a bit nasty, improvements welcome.
            {
                await Task.Delay(250);
                
                var getExecutionHistoryResponse = await client.GetExecutionHistoryAsync(getExecutionHistoryRequest);

                var historyEvent = getExecutionHistoryResponse.Events.Last();

                if (historyEvent.ExecutionSucceededEventDetails != null)
                {
                    Console.WriteLine("Execution succeeded");
                    Console.WriteLine(historyEvent.ExecutionSucceededEventDetails.Output);
                    break;
                }

                if (historyEvent.ExecutionFailedEventDetails != null)
                {
                    Console.WriteLine("Execution failed");
                    Console.WriteLine(historyEvent.ExecutionFailedEventDetails.Cause);
                    break;
                }
            }

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

            var hostBuilder =  new HostBuilder()
                .UseConsoleLifetime()
                .ConfigureServices(services =>
                {
                    services.AddSingleton(context);

                    services
                        .AddSequentialHostedServices("root", r => r
                            .Host<LambdaTestHostHostedService>()
                            .Host<StepFunctionsHostedService>());
                })
                .UseSerilog(logger);

            return (hostBuilder, context);
        }
    }
}
