using System;
using System.Reflection;
using Amazon.Lambda.Core;

namespace Logicality.AWS.Lambda.TestHost
{
    public class LambdaFunction
    {
        /// <summary>
        ///     Information about a lambda function that can be invoked.
        /// </summary>
        /// <param name="name">
        ///     The name of the function.
        /// </param>
        /// <param name="functionType">
        ///     The lambda function type that will be activate.
        /// </param>
        /// <param name="handlerMethod">
        ///     The lambda function method that will be invoked.
        /// </param>
        public LambdaFunction(
            string name,
            Type functionType,
            string handlerMethod)
        {
            Name = name;
            Type = functionType;

            HandlerMethod = functionType.GetMethod(handlerMethod, BindingFlags.Public | BindingFlags.Instance)!;

            // Search to see if a Lambda serializer is registered.
            var attribute = HandlerMethod.GetCustomAttribute(typeof(LambdaSerializerAttribute)) as LambdaSerializerAttribute ??
                            functionType.Assembly.GetCustomAttribute(typeof(LambdaSerializerAttribute)) as LambdaSerializerAttribute;

            if (attribute != null)
            {
                Serializer = (Activator.CreateInstance(attribute.SerializerType) as ILambdaSerializer)!;
            }
        }

        internal Type Type { get; }

        internal string Name { get; }

        internal MethodInfo HandlerMethod { get; }

        internal ILambdaSerializer? Serializer { get; }
    }
}