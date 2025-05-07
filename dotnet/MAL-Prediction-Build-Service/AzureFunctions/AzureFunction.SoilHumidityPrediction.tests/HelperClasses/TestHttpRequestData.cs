using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace AzureFunction.SoilHumidityPrediction.tests.HelperClasses;

public class TestHttpRequestData : HttpRequestData
{
    private readonly Stream _body;

    public TestHttpRequestData(FunctionContext context, Stream body) : base(context) {
        _body = body;
    }

    public override Stream Body => _body;
    public override HttpHeadersCollection Headers => new HttpHeadersCollection();
    public override IReadOnlyCollection<IHttpCookie> Cookies => Array.Empty<IHttpCookie>();
    public override Uri Url => new Uri("http://localhost");
    public override IEnumerable<ClaimsIdentity> Identities => Array.Empty<ClaimsIdentity>();
    public override string Method => "POST";

    public override HttpResponseData CreateResponse() {
        return new TestHttpResponseData(FunctionContext); // Let the response be created dynamically
    }
}