using System;

namespace Logicality.AWS.Lambda.TestHost
{
    internal class LambdaInstance
    {
        public LambdaInstance(LambdaFunctionInfo lambdaFunction)
        {
            LambdaFunction = lambdaFunction;
            FunctionInstance = Activator.CreateInstance(lambdaFunction.Type)!;
            Created = DateTime.Now;
            InstanceId = Guid.NewGuid();
        }

        public Guid InstanceId { get; }

        public LambdaFunctionInfo LambdaFunction { get; }

        public object FunctionInstance { get; }

        public DateTime Created { get; }
    }
}