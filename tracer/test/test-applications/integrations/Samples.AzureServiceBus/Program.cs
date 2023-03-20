using System;
using System.Threading.Tasks;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using Azure.Storage.Blobs;

namespace Samples.AzureServiceBus;

public class Program
{

    private static Tracer _tracer;

    private static async Task Main(string[] args)
    {
        var serviceName = "Samples.AzureServiceBus";
        var serviceVersion = "1.0.x";

        // Enable Azure Service Bus experimental OTEL support
        AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .AddSource(serviceName)
            .AddSource("Azure.*")
            .AddHttpClientInstrumentation()
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
            .AddConsoleExporter()
            .AddOtlpExporterIfEnvironmentVariablePresent()
            .Build();

        _tracer = tracerProvider.GetTracer(serviceName); // The version is omitted so the ActivitySource.Version / otel.library.version is not set

        using var rootSpan = _tracer.StartActiveSpan("RootSpan");
        await Task.Delay(100);

        var blobClient = new BlobClient(new Uri("https://azurestoragesamples.blob.core.windows.net/samples/cloud.jpg"));
        blobClient.DownloadTo("hello.jpg");

#if NETFRAMEWORK
        // SyncHelpers.SendAndReceiveMessages(sqsClient);
#endif
        // await AsyncHelpers.SendAndReceiveMessages(sqsClient);
    }

    /*

    private static AmazonSQSClient GetAmazonSQSClient()
    {
        if (Environment.GetEnvironmentVariable("AWS_ACCESSKEY") is string accessKey &&
            Environment.GetEnvironmentVariable("AWS_SECRETKEY") is string secretKey &&
            Environment.GetEnvironmentVariable("AWS_REGION") is string region)
        {
            var awsCredentials = new BasicAWSCredentials(accessKey, secretKey);
            return new AmazonSQSClient(awsCredentials, Amazon.RegionEndpoint.GetBySystemName(region));
        }
        else
        {
            var awsCredentials = new BasicAWSCredentials("x", "x");
            var sqsConfig = new AmazonSQSConfig { ServiceURL = "http://" + Host() };
            return new AmazonSQSClient(awsCredentials, sqsConfig);
        }
    }
    */

    private static string Host()
    {
        return Environment.GetEnvironmentVariable("AWS_SQS_HOST") ?? "localhost:9324";
    }
}
