using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using Microsoft.Extensions.ObjectPool;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace Logicality.AWS.Lambda.TestHost
{
    public class LambdaTestHostTests: IAsyncLifetime
    {
        private readonly ITestOutputHelper _outputHelper;
        private LambdaTestHost _testHost;

        public LambdaTestHostTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task Can_invoke_lambda_function()
        {
            var awsCredentials = new BasicAWSCredentials("not", "used");
            var lambdaConfig = new AmazonLambdaConfig
            {
                ServiceURL = _testHost.ServiceURL.ToString()
            };
            var lambdaClient = new AmazonLambdaClient(awsCredentials, lambdaConfig);

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
            var payload = JsonSerializer.Serialize(request);
            var invokeRequest = new InvokeRequest
            {
                FunctionName = "AWSServerless1",
                InvocationType = InvocationType.RequestResponse,
                Payload = payload,
            };
            var invokeResponse = await lambdaClient.InvokeAsync(invokeRequest);

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
                new LambdaFunction(
                    "AWSServerless1",
                    typeof(AWSServerless1.Functions),
                    nameof(AWSServerless1.Functions.Get)));
            _testHost = await LambdaTestHost.Start(settings);
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
