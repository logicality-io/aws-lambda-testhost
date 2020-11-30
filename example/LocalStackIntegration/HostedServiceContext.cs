using System;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;

namespace LocalStackIntegration
{
    public class HostedServiceContext
    {
        private readonly Dictionary<string, IHostedService> _hostedServices = new Dictionary<string, IHostedService>();

        public LocalStackHostedService LocalStack
        {
            get => Get<LocalStackHostedService>(nameof(LocalStack));
            set => Add(nameof(LocalStack), value);
        }

        public LambdaTestHostHostedService LambdaTestHost
        {
            get => Get<LambdaTestHostHostedService>(nameof(LambdaTestHost));
            set => Add(nameof(LambdaTestHost), value);
        }

        private void Add(string name, IHostedService hostedService)
        {
            lock (_hostedServices)
            {
                _hostedServices.Add(name, hostedService);
            }
        }

        private T Get<T>(string name) where T : IHostedService
        {
            lock (_hostedServices)
            {
                if (!_hostedServices.TryGetValue(name, out var value))
                {
                    throw new InvalidOperationException($"HostedService {name} was not found. Check the hosted services registration sequence.");
                }
                return (T)value;
            }
        }
    }
}