using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Samples.AzureStorageBlobs;

public class Program
{
    private static readonly string ConnectionString = Environment.GetEnvironmentVariable("BLOBS_CONNECTION_STRING");
    private static string ContainerPrefix = string.Empty;

    private static async Task Main(string[] args)
    {
        if (args.Length > 0)
        {
            ContainerPrefix = NormalizePrefix(args[0]);
        }

        var serviceName = Environment.GetEnvironmentVariable("DD_SERVICE") ?? "Samples.AzureServiceBus";
        var serviceVersion = Environment.GetEnvironmentVariable("DD_VERSION") ?? "1.0.0";
        AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

        using var tracerProvider = Sdk.CreateTracerProviderBuilder()
            .SetResourceBuilder(
                ResourceBuilder.CreateDefault()
                    .AddService(serviceName: serviceName, serviceVersion: serviceVersion))
            .AddSource(serviceName)
            .AddAzureServiceBusIfEnvironmentVariablePresent()
            .AddOtlpExporterIfEnvironmentVariablePresent()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter()
            .Build();

        var tracer = tracerProvider.GetTracer(serviceName);

        BlobServiceClient blobServiceClient = new BlobServiceClient(ConnectionString);
        await TestBlobServiceClientAsync(blobServiceClient, $"{ContainerPrefix}test-container-1");

        BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient($"{ContainerPrefix}test-container-2");

        try
        {
            await TestBlobContainerClientAsync(blobContainerClient);

            BlobClient blobClient = blobContainerClient.GetBlobClient("TestBlobClientAsync.txt");
            await TestBlobClientAsync(blobClient);
        }
        finally
        {
            await blobContainerClient.DeleteAsync();
            await blobContainerClient.DeleteIfExistsAsync(); // Creates an additional BlobContainerClient.Delete inner span
        }
    }

    // Expectation: All BlobContainerClient operations create individual traces with 3 spans, except the noted operations that create 4 spans
    // Skipped the following API's:
    // - GetAccountInfo / GetAccountInfoAsync
    // - GetProperties / GetPropertiesAsync
    // - GetStatistics / GetStatisticsAsync
    // - GetUserDelegationKey / GetUserDelegationKeyAsync
    // - SetProperties / SetPropertiesAsync
    // - UndeleteBlobContainer / UndeleteBlobContainerAsync
    private static async Task TestBlobServiceClientAsync(BlobServiceClient blobServiceClient, string containerName)
    {
        try
        {
            await blobServiceClient.CreateBlobContainerAsync(containerName);

            await foreach (TaggedBlobItem blob in blobServiceClient.FindBlobsByTagsAsync("env = 'test'"))
            {
                Console.WriteLine($"[TestBlobServiceClientAsync] FindBlobsByTagsAsync: BlobName={blob.BlobName}, BlobContainerName={blob.BlobContainerName}");
            }

            await foreach (BlobContainerItem blob in blobServiceClient.GetBlobContainersAsync())
            {
                Console.WriteLine($"[TestBlobServiceClientAsync] GetBlobContainersAsync: Name={blob.Name}");
            }
        }
        finally
        {
            await blobServiceClient.DeleteBlobContainerAsync(containerName);
        }
    }

    // Expectation: All BlobContainerClient operations create individual traces with 3 spans, except the noted operations that create 4 spans
    private static async Task TestBlobContainerClientAsync(BlobContainerClient blobContainerClient)
    {
        await blobContainerClient.CreateAsync();
        await blobContainerClient.CreateIfNotExistsAsync(); // Creates an additional BlobContainerClient.Create inner span
        await blobContainerClient.ExistsAsync(); // Creates an additional BlobContainerClient.GetProperties inner span
        await blobContainerClient.GetPropertiesAsync();
        await blobContainerClient.SetMetadataAsync(new Dictionary<string, string>() { { "language", "dotnet" } });
        await blobContainerClient.GetAccessPolicyAsync();
        await blobContainerClient.SetAccessPolicyAsync();

        const string blob1Name = "blob-1";
        await blobContainerClient.UploadBlobAsync(blob1Name, new BinaryData("blob1value"));
        await foreach (var blob in blobContainerClient.GetBlobsAsync())
        {
            Console.WriteLine($"[GetBlobsAsync] BlobName:{blob.Name}");
        }

        await foreach (var hierarchyItem in blobContainerClient.GetBlobsByHierarchyAsync(delimiter: "-"))
        {
            var isPrefix = hierarchyItem.IsPrefix;
            var isBlob = hierarchyItem.IsBlob;
            var description = isPrefix ? $"Prefix={hierarchyItem.Prefix}" : $"Blob.Name={hierarchyItem.Blob.Name}";

            Console.WriteLine($"[TestBlobContainerClientAsync] GetBlobsByHierarchyAsync: IsPrefix={isPrefix}");
            Console.WriteLine($"[TestBlobContainerClientAsync] GetBlobsByHierarchyAsync: IsBlob={isBlob}");
            Console.WriteLine($"[TestBlobContainerClientAsync] GetBlobsByHierarchyAsync: {description}");
        }

        await foreach (TaggedBlobItem blob in blobContainerClient.FindBlobsByTagsAsync("env = 'test'"))
        {
            Console.WriteLine($"[TestBlobContainerClientAsync] FindBlobsByTagsAsync: BlobName={blob.BlobName}, BlobContainerName={blob.BlobContainerName}");
        }

        await blobContainerClient.DeleteBlobAsync(blob1Name);
        await blobContainerClient.DeleteBlobIfExistsAsync(blob1Name);
    }

    // Expectation: All BlobContainerClient operations create individual traces with 3 spans, except the noted operations that create 4 spans
    // Skipped the following API's:
    // - SyncCopyFromUri / SyncCopyFromUriAsync
    // - StartSyncCopyFromUri / StartSyncCopyFromUriAsync
    // - AbortSyncCopyFromUri / AbortSyncCopyFromUriAsync
    // - DeleteImmutabilityPolicy / DeleteImmutabilityPolicyAsync
    // - SetImmutabilityPolicy / SetImmutabilityPolicyAsync
    // - SetLegalHold / SetLegalHoldAsync
    // - Undelete / UndeleteAsync
    private static async Task TestBlobClientAsync(BlobClient blobClient)
    {
        await blobClient.UploadAsync(new BinaryData("initial-value"));
        await blobClient.CreateSnapshotAsync();

        string localPath = "data";
        Directory.CreateDirectory(localPath);
        string fileName = "TestBlobClientAsync.txt";
        string localFilePath = Path.Combine(localPath, fileName);

        // API: OpenWriteAsync
        // In the BlockBlobClient.OpenWrite trace, creates an additional BlockBlobClient.Upload inner span
        // This also creates two additional traces:
        // 1. BlockBlobClient.StageBlock
        // 2. BlockBlobClient.CommitBlockList
        using (var writeStream = await blobClient.OpenWriteAsync(overwrite: true))
        {
            var bytes = Encoding.UTF8.GetBytes("write-value");
            await writeStream.WriteAsync(bytes, 0, bytes.Length);
        }

        // API: OpenReadAsync
        // Creates an additional BlobBaseClient.GetProperties inner span
        // This also creates one additional traces:
        // 1. BlobBaseClient.OpenRead
        using (var readStream = await blobClient.OpenReadAsync())
        {
            using (var fs = new FileStream(Path.Combine(localPath, "OpenReadAsync.txt"), FileMode.Create))
            {
                await readStream.CopyToAsync(fs);
            }
        }

        // API: DownloadToAsync
        // Creates an additional BlobBaseClient.DownloadStreaming inner span
        using (var fs = new FileStream(Path.Combine(localPath, "DownloadToAsync.txt"), FileMode.Create))
        {
            await blobClient.DownloadToAsync(fs);
        }

        // API: DownloadContentAsync
        var downloadContentResult = await blobClient.DownloadContentAsync();

        // API: DownloadStreamingAsync
        using (var fs = new FileStream(Path.Combine(localPath, "DownloadStreamingAsync.txt"), FileMode.Create))
        {
            var result = await blobClient.DownloadStreamingAsync();
            result.Value.Content.CopyTo(fs);
        }

        Console.WriteLine($"[TestBlobClientAsync] OpenReadAsync: {File.ReadAllText(Path.Combine(localPath, "OpenReadAsync.txt"))}");
        Console.WriteLine($"[TestBlobClientAsync] DownloadToAsync: {File.ReadAllText(Path.Combine(localPath, "DownloadToAsync.txt"))}");
        Console.WriteLine($"[TestBlobClientAsync] DownloadContentAsync: {downloadContentResult.Value.Content.ToString()}");
        Console.WriteLine($"[TestBlobClientAsync] DownloadStreamingAsync: {File.ReadAllText(Path.Combine(localPath, "DownloadStreamingAsync.txt"))}");

        await blobClient.ExistsAsync();
        await blobClient.GetPropertiesAsync();
        await blobClient.GetTagsAsync();

        await blobClient.SetAccessTierAsync(AccessTier.Hot);
        await blobClient.SetHttpHeadersAsync();
        await blobClient.SetMetadataAsync(new Dictionary<string, string> { { "content_type", "text" } });
        await blobClient.SetTagsAsync(new Dictionary<string, string> { { "env", "test" } });

        await blobClient.DeleteAsync(DeleteSnapshotsOption.OnlySnapshots);
        await blobClient.DeleteIfExistsAsync();
    }

    private static string NormalizePrefix(string input)
    {
        StringBuilder sb = new();
        
        foreach (char c in input)
        {
            switch (c)
            {
                case (>= 'a' and <= 'z') or (>= '0' and <= '9') or '-':
                    sb.Append(c);
                    break;
                case (>= 'A' and <= 'Z'):
                    sb.Append(char.ToLower(c));
                    break;
                default:
                    break;
            }
        }

        return sb.ToString();
    }
}
