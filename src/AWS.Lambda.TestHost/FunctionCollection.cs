using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Logicality.AWS.Lambda.TestHost
{
    public class FunctionCollection
    {
        private readonly Dictionary<string, LambdaFunction> _functions = new Dictionary<string, LambdaFunction>();
        private readonly ReadOnlyDictionary<string, LambdaFunction> _functionsReadOnly;

        public FunctionCollection()
        {
            _functionsReadOnly = new ReadOnlyDictionary<string, LambdaFunction>(_functions);
        }

        public FunctionCollection Add(string name, Type functionType, string handlerMethod)
        {
            var functionInfo = new LambdaFunction(name, functionType, handlerMethod);
            _functions.Add(functionInfo.Name, functionInfo);
            return this;
        }

        internal IReadOnlyDictionary<string, LambdaFunction> Functions => _functionsReadOnly;
    }
}