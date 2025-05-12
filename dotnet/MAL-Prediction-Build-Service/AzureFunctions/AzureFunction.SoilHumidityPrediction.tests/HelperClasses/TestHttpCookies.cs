using Microsoft.Azure.Functions.Worker.Http;

namespace AzureFunction.SoilHumidityPrediction.tests.HelperClasses;

public class TestHttpCookies : HttpCookies
{
    private readonly List<IHttpCookie> _cookies = new();

    public override void Append(string name, string value) {
        throw new NotImplementedException();
    }

    public override void Append(IHttpCookie cookie) => _cookies.Add(cookie);
    public override IHttpCookie CreateNew() {
        throw new NotImplementedException();
    }

    public IReadOnlyCollection<IHttpCookie> GetCookies() => _cookies.AsReadOnly();
}