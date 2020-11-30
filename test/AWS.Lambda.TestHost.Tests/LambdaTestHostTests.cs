using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Runtime;
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
        public async Task Can_invoke_lambda_function()
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
            var invokeResponse = await _lambdaClient.InvokeRequestAsync("AWSServerless1", request);

            invokeResponse.StatusCode.ShouldBe(200);
            invokeResponse.Payload.Length.ShouldBeGreaterThan(0);

            var streamReader = new StreamReader(invokeResponse.Payload);
            var payloadJson = await streamReader.ReadToEndAsync();

            var apiGatewayProxyResponse = JsonSerializer.Deserialize<APIGatewayProxyResponse>(payloadJson);
            apiGatewayProxyResponse.IsBase64Encoded.ShouldBeFalse();
            apiGatewayProxyResponse.Body.ShouldNotBeNullOrWhiteSpace();

            //_outputHelper.WriteLine(invokeResponse.LogResult);
        }

        public async Task InitializeAsync()
        { 
            var settings = new LambdaTestHostSettings(() => new TestLambdaContext
            {
                Logger = new XunitLambdaLogger(_outputHelper)
            });
            settings.AddFunction(
                new LambdaFunctionInfo(
                    "AWSServerless1",
                    typeof(AWSServerless1.Function),
                    nameof(AWSServerless1.Function.Get)));
            _testHost = await LambdaTestHost.Start(settings);

            var awsCredentials = new BasicAWSCredentials("not", "used");
            var lambdaConfig = new AmazonLambdaConfig
            {
                ServiceURL = _testHost.ServiceURL.ToString()
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
