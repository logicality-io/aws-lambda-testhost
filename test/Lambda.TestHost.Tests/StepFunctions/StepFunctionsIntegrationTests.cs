using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;
using Xunit;
using Xunit.Abstractions;

namespace Logicality.AWS.Lambda.TestHost.StepFunctions
{
    public class StepFunctionsIntegrationTests: IAsyncLifetime
    {
        private readonly ITestOutputHelper _outputHelper;
        private LambdaTestHost _lambdaTestHost;
        private IContainerService _containerService;
        private Uri _stepFunctionsServiceUrl;
        private const int ContainerPort = 8083;
        public const int Port = 8083;

        public StepFunctionsIntegrationTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task Can_be_invoked_from_step_functions_local()
        {
            // 1. Create the StepFunctions Client
            var credentials = new BasicAWSCredentials("not", "used");
            var config = new AmazonStepFunctionsConfig
            {
                ServiceURL = _stepFunctionsServiceUrl.ToString()
            };
            var client = new AmazonStepFunctionsClient(credentials, config);

            // 2. Create step machine with a single task the invokes a lambda function
            // The FunctionName contains the lambda to be invoked.
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

            // 4. Poll and wait for the 
            while (true)
            {
                var getExecutionHistoryResponse = await client.GetExecutionHistoryAsync(getExecutionHistoryRequest);

                var historyEvent = getExecutionHistoryResponse.Events.Last();

                if (historyEvent.ExecutionSucceededEventDetails != null)
                {
                    _outputHelper.WriteLine("Execution succeeded");
                    _outputHelper.WriteLine(historyEvent.ExecutionSucceededEventDetails.Output);
                    break;
                }

                if (historyEvent.ExecutionFailedEventDetails != null)
                {
                    _outputHelper.WriteLine("Execution failed");
                    _outputHelper.WriteLine(historyEvent.ExecutionFailedEventDetails.Cause);
                    break;
                }
            }
        }

        public async Task InitializeAsync()
        {
            var settings = new LambdaTestHostSettings(() => new TestLambdaContext());
            settings.AddFunction(new LambdaFunctionInfo(
                nameof(SimpleLambdaFunction),
                typeof(SimpleLambdaFunction),
                nameof(SimpleLambdaFunction.FunctionHandler)));
            _lambdaTestHost = await LambdaTestHost.Start(settings);

            var url = new UriBuilder(_lambdaTestHost.ServiceUrl)
            {
                Host = Environment.OSVersion.Platform == PlatformID.Win32NT
                    ? "host.docker.internal"
                    : "172.17.0.1"
            };

            _containerService = new Builder()
                .UseContainer()
                .WithName("lambda-testhost-stepfunctions")
                .UseImage("amazon/aws-stepfunctions-local:latest")
                .WithEnvironment(
                    $"LAMBDA_ENDPOINT={url}")
                .ReuseIfExists()
                .ExposePort(Port, ContainerPort)
                .WaitForPort($"{ContainerPort}/tcp", 10000, "127.0.0.1")
                .Build()
                .Start();

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
