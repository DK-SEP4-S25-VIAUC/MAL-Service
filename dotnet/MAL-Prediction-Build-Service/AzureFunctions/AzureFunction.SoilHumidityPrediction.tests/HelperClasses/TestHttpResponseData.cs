using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AzureFunction.SoilHumidityPrediction.tests.HelperClasses;

public class TestHttpResponseData : HttpResponseData
{
    private readonly MemoryStream _bodyStream = new();
    private readonly StreamWriter _writer;
    private HttpHeadersCollection _headers;

    public TestHttpResponseData(FunctionContext functionContext) : base(functionContext) {
        StatusCode = HttpStatusCode.OK;
        Headers = new HttpHeadersCollection();
        Body = _bodyStream;
        _writer = new StreamWriter(_bodyStream) { AutoFlush = true };
        Cookies = new TestHttpCookies();
    }

    public override HttpHeadersCollection Headers
    {
        get => _headers;
        set => _headers = value ?? new HttpHeadersCollection();
    }

    public override Stream Body { get; set; }

    public override HttpCookies Cookies { get; }

    public override HttpStatusCode StatusCode { get; set; }

    public async Task WriteStringAsync(string text, CancellationToken cancellationToken = default) {
        await _writer.WriteAsync(text);
        await _writer.FlushAsync();
        _bodyStream.Position = 0; // Reset for reading later
    }
}