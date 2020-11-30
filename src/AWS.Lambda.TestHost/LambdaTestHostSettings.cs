using System;
using System.Collections.Generic;
using Amazon.Lambda.Core;

namespace Logicality.AWS.Lambda.TestHost
{
    public class LambdaTestHostSettings
    {
        /// <summary>
        /// The URL the lambda test host will listen on. Default value is http://127.0.0.1:0
        /// which will listen on a random free port. To get the URL to invoke lambdas, use
        /// LambdaTestHost.ServiceUrl.
        /// </summary>
        public string WebHostUrl { get; set; } = "http://127.0.0.1:0";

        public LambdaTestHostSettings(Func<ILambdaContext> createContext)
        {
            CreateContext = createContext;
        }

        /// <summary>
        /// Gets or sets the maximum concurrency limit for all hosted lambdas.
        /// </summary>
        public uint ConcurrencyLimit { get; set; } = 1000;

        internal Func<ILambdaContext> CreateContext { get; }

        internal Dictionary<string, LambdaFunction> Functions { get; }
            = new Dictionary<string, LambdaFunction>(StringComparer.OrdinalIgnoreCase);

        public void AddFunction(LambdaFunction lambdaFunction)
        {
            Functions.Add(lambdaFunction.Name, lambdaFunction);
        }
    }
}