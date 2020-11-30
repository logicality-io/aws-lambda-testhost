using System.Collections.Generic;
using Shouldly;
using Xunit;

namespace Logicality.AWS.Lambda.TestHost
{
    public class LambdaAccountPoolTests
    {
        [Fact]
        public void Can_get_lambda_instance()
        {
            var lambdaFunctionInfos = new Dictionary<string, LambdaFunctionInfo>();
            var lambdaFunctionInfo = new LambdaFunctionInfo(
                "test",
                typeof(AWSServerless1.Function),
                nameof(AWSServerless1.Function.Get));
            lambdaFunctionInfos.Add(lambdaFunctionInfo.Name, lambdaFunctionInfo);
            var lambdaAccountPool = new LambdaAccountPool(1000, lambdaFunctionInfos);

            var lambdaInstance = lambdaAccountPool.Get("test");

            lambdaInstance.ShouldNotBeNull();
        }

        [Fact]
        public void When_limiting_instance_concurrency_exceeded_then_should_not_get_an_instance()
        {
            var lambdaFunctionInfos = new Dictionary<string, LambdaFunctionInfo>();
            var lambdaFunctionInfo = new LambdaFunctionInfo(
                "test",
                typeof(AWSServerless1.Function),
                nameof(AWSServerless1.Function.Get),
                1);
            lambdaFunctionInfos.Add(lambdaFunctionInfo.Name, lambdaFunctionInfo);
            var lambdaAccountPool = new LambdaAccountPool(1000, lambdaFunctionInfos);
            var lambdaInstance1 = lambdaAccountPool.Get("test");

            var lambdaInstance2 = lambdaAccountPool.Get("test");

            lambdaInstance1.ShouldNotBeNull();
            lambdaInstance2.ShouldBeNull();
        }

        [Fact]
        public void When_limiting_account_concurrency_exceeded_then_should_not_get_an_instance()
        {
            var lambdaFunctionInfos = new Dictionary<string, LambdaFunctionInfo>();
            var lambdaFunctionInfo = new LambdaFunctionInfo(
                "test",
                typeof(AWSServerless1.Function),
                nameof(AWSServerless1.Function.Get));
            lambdaFunctionInfos.Add(lambdaFunctionInfo.Name, lambdaFunctionInfo);
            var lambdaAccountPool = new LambdaAccountPool(1, lambdaFunctionInfos);
            var lambdaInstance1 = lambdaAccountPool.Get("test");

            var lambdaInstance2 = lambdaAccountPool.Get("test");

            lambdaInstance1.ShouldNotBeNull();
            lambdaInstance2.ShouldBeNull();
        }

        [Fact]
        public void Can_return_an_instance()
        {
            var lambdaFunctionInfos = new Dictionary<string, LambdaFunctionInfo>();
            var lambdaFunctionInfo = new LambdaFunctionInfo(
                "test",
                typeof(AWSServerless1.Function),
                nameof(AWSServerless1.Function.Get));
            lambdaFunctionInfos.Add(lambdaFunctionInfo.Name, lambdaFunctionInfo);
            var lambdaAccountPool = new LambdaAccountPool(1, lambdaFunctionInfos);
            var lambdaInstance1 = lambdaAccountPool.Get("test");

            lambdaAccountPool.Return(lambdaInstance1!);

            var lambdaInstance2 = lambdaAccountPool.Get("test");

            lambdaInstance2.ShouldBe(lambdaInstance1);
        }
    }
}
