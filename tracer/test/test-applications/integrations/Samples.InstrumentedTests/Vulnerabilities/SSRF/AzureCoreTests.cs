using Xunit;
using RestSharp;
using System.Threading;
using System.Threading.Tasks;
using System;
using Azure.Core.Pipeline;
using Azure.Core;
using System.IO;
using Azure.Storage.Blobs;
using System.Net;

namespace Samples.InstrumentedTests.Iast.Vulnerabilities.SSRF;

public class AzureCoreTests : SSRFTests
{
    [Fact]
    public void GivenAAzureCoreMessage_WhenProcess_Vulnerable()
    {
        HttpPipeline pipeline = new HttpPipeline(HttpClientTransport.Shared);

        var request = pipeline.CreateRequest();
        var builder = new RequestUriBuilder();
        builder.Reset(new Uri(taintedUrlValue));
        request.Uri = builder;
        ResponseClassifier responseClassifier = new StatusCodeClassifier(new ReadOnlySpan<ushort> ());
        var message = new HttpMessage(request, responseClassifier);
        var transport = new HttpClientTransport();
        transport.Process(message);
        AssertVulnerable("SSRF", sourceType: sourceType);
    }

    [Fact]
    public void GivenAAzureCoreMessage_WhenProcessAsync_Vulnerable()
    {
        HttpPipeline pipeline = new HttpPipeline(HttpClientTransport.Shared);

        var request = pipeline.CreateRequest();
        var builder = new RequestUriBuilder();
        builder.Reset(new Uri(taintedUrlValue));
        request.Uri = builder;
        ResponseClassifier responseClassifier = new StatusCodeClassifier(new ReadOnlySpan<ushort>());
        var message = new HttpMessage(request, responseClassifier);
        var transport = new HttpClientTransport();
        transport.ProcessAsync(message);
        AssertVulnerable("SSRF", sourceType: sourceType);
    }

    [Fact]
    public void GivenAAzureCoreRequest_WhenCreateRequest_Vulnerable()
    {
        var blobClient = new BlobClient(new Uri(taintedUrlValue));
        using (var stream = new MemoryStream())
        {
            blobClient.Upload(stream);
        }
        AssertVulnerable("SSRF", sourceType: sourceType);
    }

    [Fact]
    public void GivenAAzureCoreRequest_WhenCreateRequest_Vulnerableg()
    {
        RequestUriBuilder builder = new RequestUriBuilder();
        builder.AppendPath(taintedUrlValue);
        var result = builder.ToString();
        AssertTainted(result);
    }

    [Fact]
    public void GivenAAzureCoreRequest_WhenCreateRequest_Vulnerable3()
    {
        var blobClient = new BlobClient(new Uri(taintedUrlValue));
        AssertTainted(blobClient.Uri.ToString());
    }

    [Fact]
    public void GivenAAzureCoreMessage_WhenProcessAsync_Vulnerableresdef()
    {
        HttpPipeline pipeline = new HttpPipeline(HttpClientTransport.Shared);
        var messageRequest = pipeline.CreateRequest();
        var builder = new RequestUriBuilder();
        builder.Reset(new Uri(taintedUrlValue));
        messageRequest.Uri = builder;
        var request = WebRequest.CreateHttp(messageRequest.Uri.ToUri());
        AssertTainted(request.RequestUri.ToString());
    }
}
