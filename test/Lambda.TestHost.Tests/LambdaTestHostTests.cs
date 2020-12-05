using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using Logicality.AWS.Lambda.TestHost.Functions;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Logicality.AWS.Lambda.TestHost
{
    public class LambdaTestHostTests: IAsyncLifetime
    {
        private readonly ITestOutputHelper _outputHelper;
        private LambdaTestHost _testHost;
        private AmazonLambdaClient _lambdaClient;

        public LambdaTestHostTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task Can_invoke_api_gateway_lambda_function()
        {
            var request = new APIGatewayProxyRequest
            {
                Body = "{ \"a\" = \"b\" }",
                HttpMethod = "GET",
                Path = "/foo/bar",
                RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
                {
                    HttpMethod = "GET",
                    Path = "/foo/bar"
                }
            };
            var invokeResponse = await _lambdaClient.InvokeRequestAsync("APIGatewayFunction", request);

            invokeResponse.StatusCode.ShouldBe(200);
            invokeResponse.Payload.Length.ShouldBeGreaterThan(0);

            var streamReader = new StreamReader(invokeResponse.Payload);
            var payload = await streamReader.ReadToEndAsync();

            var apiGatewayProxyResponse = JsonSerializer.Deserialize<APIGatewayProxyResponse>(payload);
            apiGatewayProxyResponse.IsBase64Encoded.ShouldBeFalse();
            apiGatewayProxyResponse.Body.ShouldNotBeNullOrWhiteSpace();
        }


        [Fact]
        public async Task Can_invoke_simple_lambda_function()
        {
            var invokeRequest = new InvokeRequest
            {
                InvocationType = InvocationType.RequestResponse,
                Payload = "\"string\"",
                FunctionName = "ReverseStringFunction",
            };
            var invokeResponse = await _lambdaClient.InvokeAsync(invokeRequest);

            invokeResponse.StatusCode.ShouldBe(200);
            invokeResponse.Payload.Length.ShouldBeGreaterThan(0);

            var streamReader = new StreamReader(invokeResponse.Payload);
            var payload = await streamReader.ReadToEndAsync();

            payload.ShouldBe("\"gnirts\"");
        }

        public async Task InitializeAsync()
        { 
            var settings = new LambdaTestHostSettings(() => new TestLambdaContext
            {
                Logger = new XunitLambdaLogger(_outputHelper)
            });
            settings.AddFunction(
                new LambdaFunctionInfo(
                    "ReverseStringFunction",
                    typeof(ReverseStringFunction),
                    nameof(ReverseStringFunction.Reverse)));

            settings.AddFunction(
                new LambdaFunctionInfo(
                    "APIGatewayFunction",
                    typeof(APIGatewayFunction),
                    nameof(APIGatewayFunction.Handle)));

            _testHost = await LambdaTestHost.Start(settings);

            var awsCredentials = new BasicAWSCredentials("not", "used");
            var lambdaConfig = new AmazonLambdaConfig
            {
                ServiceURL = _testHost.ServiceUrl.ToString()
            };
            _lambdaClient = new AmazonLambdaClient(awsCredentials, lambdaConfig);
        }

        public async Task DisposeAsync() 
            => await _testHost.DisposeAsync();

        private class XunitLambdaLogger : ILambdaLogger
        {
            private readonly ITestOutputHelper _outputHelper;

            public XunitLambdaLogger(ITestOutputHelper outputHelper)
            {
                _outputHelper = outputHelper;
            }

            public void Log(string message) => _outputHelper.WriteLine(message);

            public void LogLine(string message) => _outputHelper.WriteLine(message);
        }
    }
}
