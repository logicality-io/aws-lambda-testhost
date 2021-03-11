using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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

        [Fact(Skip = "Known to fail.")]
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

            // Fails:
            // 2021-03-11T09:01:18:WARNING:bootstrap.py: Thread run method <function AdaptiveThreadPool.submit.<locals>._run at 0x7f1ed537d160>(None) failed:
            // Unable to find listener for service "lambda" - please make sure to include it in $SERVICES
            //
            // Expectation: should the edge router just forward to the LAMBDA_FORWARD_URL regardless of whether lambda service is started or not?
        }

        [Fact(Skip = "Known to fail.")]
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

            // Fails
            // LocalStack logs:
            //  2021-03-11T09:45:19:INFO:localstack.services.awslambda.lambda_api: Function not found: arn:aws:lambda:us-east-1:000000000000:function:simple
            //
            // Expectation, if function cannot be found then it should be forwarded to LAMBDA_FORWARD_URL.
            // 
        }

        [Fact]
        public async Task Can_invoke_via_localstack_with_lambda_specified()
        {
            await using var fixture = await LocalStackFixture.Create(_outputHelper, "lambda");

            // 1. Create the StepFunctions Client
            var credentials = new BasicAWSCredentials("not", "used");
            var lambdaClient = new AmazonLambdaClient(credentials, new AmazonLambdaConfig
            {
                ServiceURL = fixture.ServiceUrl.ToString()
            });

            var functionInfo = fixture.LambdaTestHost.Settings.Functions.First().Value;
            var createFunctionRequest = new CreateFunctionRequest
            {
                Handler = "dummy-handler",
                FunctionName = functionInfo.Name,
                Role = "arn:aws:iam::123456789012:role/foo",
                Code = new FunctionCode
                {
                    ZipFile = new MemoryStream()
                }
            };
            await lambdaClient.CreateFunctionAsync(createFunctionRequest);

            var invokeRequest = new InvokeRequest
            {
                FunctionName = functionInfo.Name,
                Payload = "{ \"Data\": \"Bar\" }",
            };

            var invokeResponse = await lambdaClient.InvokeAsync(invokeRequest);

            invokeResponse.FunctionError.ShouldBeNullOrEmpty();

            var responsePayload = Encoding.UTF8.GetString(invokeResponse.Payload.ToArray());

            responsePayload.ShouldBe("{ \"Reverse\": \"raB\" }");

            // Fails 
            // LocalStack Logs:
            //   2021-03-11T16:52:12:DEBUG:localstack.services.awslambda.lambda_api: Forwarding Lambda invocation to LAMBDA_FORWARD_URL: http://host.docker.internal:61654
            //   2021-03-11T16:52:12:DEBUG:localstack.services.awslambda.lambda_api: Received result from external Lambda endpoint (status 200): b'{"Reverse":"raB"}'
            //
            // Payload:
            // Shouldly.ShouldAssertException: responsePayload
            //   should be
            // "{ "Reverse": "raB" }"
            //   but was
            // "<Response [200]>"
        }
    }
}
 