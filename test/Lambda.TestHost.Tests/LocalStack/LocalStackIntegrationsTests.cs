using System;
using System.Net;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Logicality.AWS.Lambda.TestHost.LocalStack
{
    public class LocalStackIntegrationsTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public LocalStackIntegrationsTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task Can_invoke_via_localstack_without_specifying_lambda_service()
        {
            await using var fixture = await LocalStackFixture.Create(_outputHelper, "");

            // 1. Create the StepFunctions Client
            var credentials = new BasicAWSCredentials("not", "used");
            var lambdaClient = new AmazonLambdaClient(credentials, new AmazonLambdaConfig
            {
                ServiceURL = fixture.ServiceUrl.ToString()
            });

            var invokeRequest = new InvokeRequest
            {
                FunctionName = "simple",
                Payload = "{}",
            };

            var invokeResponse = await lambdaClient.InvokeAsync(invokeRequest);

            invokeResponse.HttpStatusCode.ShouldBe(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Can_invoke_via_localstack_when_specifying_lambda_service()
        {
            await using var fixture = await LocalStackFixture.Create(_outputHelper, "lambda");

            // 1. Create the StepFunctions Client
            var credentials = new BasicAWSCredentials("not", "used");
            var lambdaClient = new AmazonLambdaClient(credentials, new AmazonLambdaConfig
            {
                ServiceURL = fixture.ServiceUrl.ToString()
            });

            var invokeRequest = new InvokeRequest
            {
                FunctionName = "simple",
                Payload = "{}",
            };

            var invokeResponse = await lambdaClient.InvokeAsync(invokeRequest);

            invokeResponse.HttpStatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }
}
 