using System.Text.Json;
using System.Threading.Tasks;
using Amazon.Lambda.Model;

// ReSharper disable once CheckNamespace
namespace Amazon.Lambda
{
    public static class LambdaClientExtensions
    {
        /// <summary>
        /// Serializes a request object and invokes it against the supplied function name.
        /// </summary>
        /// <typeparam name="T">The request object </typeparam>
        /// <param name="client"></param>
        /// <param name="functionName"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static Task<InvokeResponse> InvokeRequestAsync<T>(this IAmazonLambda client, string functionName, T request)
        {
            var payload = JsonSerializer.Serialize(request);
            var invokeRequest = new InvokeRequest
            {
                FunctionName = functionName,
                InvocationType = InvocationType.RequestResponse,
                Payload = payload
            };
            return client.InvokeAsync(invokeRequest);
        }
    }
}
