using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;

namespace LocalStackIntegration
{
    public class SimpleLambdaFunction
    {
        [LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]
        public Task FunctionHandler(SQSEvent request, ILambdaContext lambdaContext)
        {
            return Task.CompletedTask;
        }
    }
}