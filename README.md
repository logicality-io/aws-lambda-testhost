# AWS Lambda TestHost

A .NET host that can host and execute .NET Lambdas for simulation and debugging
purposes.

## Packages

| Name | Package | Description |
|---|---|---|
| `Logicality.AWS.Lambda.TestHost` | [![feedz.io][p1]][d1] | Main TestHost package. |

## Using

It works by running a web server that can handle lambda invocation requests,
activate the appropriate lambda class, invoking it's handler (dealing with any
serialization needs) and returning responses.

Given this simple function:

```csharp
public class ReverseStringFunction
{
    [LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]
    public string Reverse(string input, ILambdaContext context)
    {
        return new string(input.Reverse().ToArray());
    }
}
```

Create a `LambdaTestHostSettings` that supplies a `ILambdaContext` factory. You
can use the supplied `TestLambdaContext` or your own `ILambdaContext`
implementation. Then add one or more `LambdaFunctionInfo`s to the settings.

```csharp
var settings = new LambdaTestHostSettings(() => new TestLambdaContext());

settings.AddFunction(
    new LambdaFunctionInfo(
        nameof(ReverseStringFunction),
        typeof(ReverseStringFunction),
        nameof(ReverseStringFunction.Reverse)).
        reservedConcurrency: 1); // Optional: This will limit the number of concurrent invocations
```

### Comparison with AWS .NET Mock Lambda Test Tool

This is not meant to replace the [Test Tool][lambda-test-tool] but to augment it. Key differences are:

- `Test Tool` works against a single Lambda at a time.
- `Test Host` is a library you can use in Tests projects or Development servers
  and can host multiple lambdas. `Test Host` is useful for developing /
  debugging multiple lambdas at once (i.e. a Lambda "Application", StepFunctions
  etc) and exercising them with code. Test Tool is GUI and manual.
- `Test Tool` uses `AssemblyLoadContext` to prevent version conflicts with your
  code. `Test Host` uses direct references so any dependencies will be the same version.
- You can use AWSSDK Lambda client to invoke functions hosted by `Test Host`.
- Like `Test Tool`, `Test Host` is not a local Lambda Environment and thus "not
  intended to diagnose platform specific issue but instead it can be useful for
  debugging application logic issues.".

### Using with StepFunctions Local


See [`example/StepFunctionsLocal`](example/StepFunctionsLocal) for runnable example.

## Building

The standard build runs in a containerized environment and requires docker.

Windows:

```bash
.\build.cmd
```

Linux:

```bash
.\build.sh
```

Builds can be performed on local environment and requires .NET Core 3.1.x and
.NET 5.0.x SDKs to be installed.

Windows:

```bash
.\buildl.cmd
```

Linux:

```bash
.\buildl.sh
```

## Licence

MIT. Contains code from Amazon.Lambdf

## Contact

[p1]: https://img.shields.io/badge/endpoint.svg?url=https%3A%2F%2Ff.feedz.io%2Flogicality%2Fpublic%2Fshield%2FLogicality.AWS.Lambda.TestHost%2Fstable
[d1]: https://f.feedz.io/logicality/public/nuget/index.json
[lambda-test-tool]: https://github.com/aws/aws-lambda-dotnet/tree/master/Tools/LambdaTestTool
