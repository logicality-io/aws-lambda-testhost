using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace Logicality.AWS.Lambda.TestHost
{
    public class LambdaTestHost: IAsyncDisposable
    {
        private readonly IWebHost _webHost;

        private LambdaTestHost(LambdaTestHostSettings settings)
        {
            _webHost = Microsoft.AspNetCore.WebHost
                .CreateDefaultBuilder<Startup>(Array.Empty<string>())
                .UseUrls(settings.WebHostUrl)
                .ConfigureServices(services =>
                {
                    services.AddSingleton(settings);
                })
                .Build();
        }

        /// <summary>
        /// The URL that the LambdaTestHost is handling request.
        /// </summary>
        public Uri ServiceURL { get; private set; }

        public static async ValueTask<LambdaTestHost> Start(LambdaTestHostSettings settings)
        {
            var host = new LambdaTestHost(settings);
            await host._webHost.StartAsync();
            host.ServiceURL = host._webHost.GetUris().Single();
            return host;
        }

        private class Startup
        {
            private readonly LambdaTestHostSettings _settings;

            public Startup(LambdaTestHostSettings settings)
            {
                _settings = settings;
            }

            public void ConfigureServices(IServiceCollection services)
            {
                services.AddRouting();
            }

            public void Configure(IApplicationBuilder app)
            {

                app.Map("/2015-03-31/functions", functions =>
                {
                    functions.UseRouting();

                    functions.UseEndpoints(endpoints =>
                    {
                        endpoints.MapPost("/{functionName}/invocations", async ctx =>
                        {
                            var functionName = (string)ctx.Request.RouteValues["functionName"];
                            if (!_settings.Functions.TryGetValue(functionName, out var lambdaFunction))
                            {
                                ctx.Response.StatusCode = 404;
                                return;
                            }

                            try
                            {

                                var streamReader = new StreamReader(ctx.Request.Body, Encoding.UTF8);
                                var payload = await streamReader.ReadToEndAsync();

                                var instance = Activator.CreateInstance(lambdaFunction.Type);

                                var settings = ctx.RequestServices.GetRequiredService<LambdaTestHostSettings>();

                                var context = settings.CreateContext();

                                var parameters = BuildParameters(lambdaFunction, context, payload);

                                var lambdaReturnObject = lambdaFunction.HandlerMethod.Invoke(instance, parameters);
                                var responseBody = await ProcessReturnAsync(lambdaFunction, lambdaReturnObject);

                                ctx.Response.StatusCode = 200;
                                await ctx.Response.WriteAsync(responseBody);
                            }
                            catch (TargetInvocationException ex)
                            {

                            }
                            catch(Exception ex)
                            {

                            }
                        });
                    });
                });
            }

            private void GetInstance(LambdaFunction lambdaFunction)
            {

            }

            /// Adapted from Amazon.Lambda.TestTools
            private static object[] BuildParameters(LambdaFunction function, ILambdaContext context, string? payload)
            {
                var parameters = function.HandlerMethod.GetParameters();
                var parameterValues = new object[parameters.Length];

                if (parameterValues.Length > 2)
                    throw new Exception($"Method {function.HandlerMethod.Name} has too many parameters, {parameterValues.Length}. Methods called by Lambda" +
                                        $"can have at most 2 parameters. The first is the input object and the second is an ILambdaContext.");

                for (var i = 0; i < parameters.Length; i++)
                {
                    if (parameters[i].ParameterType == typeof(ILambdaContext))
                    {
                        parameterValues[i] = context;
                    }
                    else if (payload != null)
                    {
                        var stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));
                        if (function.Serializer != null)
                        {
                            var genericMethodInfo = function.Serializer.GetType()
                                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                .FirstOrDefault(x => string.Equals(x.Name, "Deserialize"));

                            var methodInfo = genericMethodInfo.MakeGenericMethod(new[]
                            {
                                parameters[i].ParameterType
                            });

                            try
                            {
                                parameterValues[i] = methodInfo.Invoke(function.Serializer, new object[] { stream });
                            }
                            catch (Exception e)
                            {
                                throw new Exception($"Error deserializing the input JSON to type {parameters[i].ParameterType.Name}", e);
                            }
                        }
                        else
                        {
                            parameterValues[i] = stream;
                        }
                    }
                }

                return parameterValues;
            }

            private static async Task<string?> ProcessReturnAsync(LambdaFunction function, object? lambdaReturnObject)
            {
                Stream? lambdaReturnStream = null;

                if (lambdaReturnObject == null)
                {
                    return null;
                }

                // If the return was a Task then wait till the task is complete.
                if (lambdaReturnObject is Task task)
                {
                    await task;

                    // Check to see if the Task returns back an object.
                    if (task.GetType().IsGenericType)
                    {
                        var resultProperty = task.GetType().GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
                        if (resultProperty != null)
                        {
                            var taskResult = resultProperty.GetMethod!.Invoke(task, null);
                            if (taskResult is Stream stream)
                            {
                                lambdaReturnStream = stream;
                            }
                            else
                            {
                                lambdaReturnStream = new MemoryStream();
                                function.Serializer!.Serialize(taskResult, lambdaReturnStream);
                            }
                        }
                    }
                }
                else
                {
                    lambdaReturnStream = new MemoryStream();
                    function.Serializer!.Serialize(lambdaReturnObject, lambdaReturnStream);
                }

                if (lambdaReturnStream == null)
                {
                    return null;
                }

                lambdaReturnStream.Position = 0;
                using var reader = new StreamReader(lambdaReturnStream);
                return await reader.ReadToEndAsync();
            }
        }

        public async ValueTask DisposeAsync() => await _webHost.StopAsync();
    }
}