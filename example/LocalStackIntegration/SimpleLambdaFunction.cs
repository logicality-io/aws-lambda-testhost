using System.Linq;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;

namespace LocalStackIntegration
{
    public class SimpleLambdaFunction
    {
        [LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]
        public string FunctionHandler(string input, ILambdaContext lambdaContext)
        {
            return new string(input.Reverse().ToArray());
        }
    }
}