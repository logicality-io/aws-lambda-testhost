using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using Ductus.FluentDocker.Builders;
using Ductus.FluentDocker.Services;
using Logicality.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LocalStackIntegration
{
    public class LocalStackHostedService : DockerHostedService
    {
        private readonly HostedServiceContext _context;
        public const int Port = 4566;
        private const int ContainerPort = 4566;

        public LocalStackHostedService(
            HostedServiceContext context,
            ILogger<DockerHostedService> logger)
            : base(logger)
        {
            _context = context;
        }

        protected override string ContainerName => "lambda-testhost-localstack";

        public Uri ServiceUrl { get; private set; }

        public AWSCredentials AWSCredentials { get; private set; }

        protected override IContainerService CreateContainerService()
            => new Builder()
                .UseContainer()
                .WithName(ContainerName)
                .UseImage("localstack/localstack:latest")
                .WithEnvironment(
                    "SERVICES=sqs,lambda",
                    $"LAMBDA_FALLBACK_URL={_context.LambdaTestHost.ServiceUrl}",
                    "LOCALSTACK_API_KEY=45svCDcHrN")
                //.UseNetwork("host")
                .ReuseIfExists()
                .ExposePort(Port, ContainerPort)
                .ExposePort(443, 443)
                .WaitForPort($"{ContainerPort}/tcp", 10000, "127.0.0.1")
                .Build();

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await base.StartAsync(cancellationToken);
            ServiceUrl = new Uri($"http://localhost:{Port}");
            _context.LocalStack = this;

            AWSCredentials = new BasicAWSCredentials("not", "used");

            await Task.Delay(5000);
            var sqsConfig = new AmazonSQSConfig
            {
                ServiceURL = ServiceUrl.ToString()
            };

            var sqsClient = new AmazonSQSClient(AWSCredentials, sqsConfig);
            var createQueueRequest = new CreateQueueRequest("test-queue");
            var createQueueResponse = await sqsClient.CreateQueueAsync(createQueueRequest, cancellationToken);
            var lambdaConfig = new AmazonLambdaConfig
            {
                ServiceURL = ServiceUrl.ToString()
            };

            var queueAttributesAsync = await sqsClient.GetQueueAttributesAsync(createQueueResponse.QueueUrl, 
                new List<string>{ QueueAttributeName.All }, cancellationToken);

            var lambdaClient = new AmazonLambdaClient(AWSCredentials, lambdaConfig);
            var createEventSourceMappingRequest = new CreateEventSourceMappingRequest
            {
                EventSourceArn = queueAttributesAsync.QueueARN,
                FunctionName = "simple"
            };
            var createEventSourceMappingResponse = await lambdaClient
                .CreateEventSourceMappingAsync(createEventSourceMappingRequest, cancellationToken);

            var sendMessageRequest = new SendMessageRequest(createQueueResponse.QueueUrl, "test");
            await sqsClient.SendMessageAsync(sendMessageRequest, cancellationToken);
        }
    }
}