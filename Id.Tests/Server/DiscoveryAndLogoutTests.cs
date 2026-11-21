using System.Net;
using System.Text.Json;
using Id.Tests.Infrastructure;

namespace Id.Tests.Server;

public class DiscoveryAndLogoutTests(IdServerFactory app)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Discovery_advertises_only_what_is_implemented()
    {
        using var b = app.NewBrowser();
        var doc = JsonDocument.Parse(await b.Http.GetStringAsync("/.well-known/openid-configuration", Ct)).RootElement;
        Assert.Equal(IdServerFactory.Issuer, doc.GetProperty("issuer").GetString());
        Assert.Equal($"{IdServerFactory.Issuer}/connect/token", doc.GetProperty("token_endpoint").GetString());
        Assert.Equal(["code"], doc.GetProperty("response_types_supported").EnumerateArray().Select(e => e.GetString()));
        Assert.Equal(["S256"], doc.GetProperty("code_challenge_methods_supported").EnumerateArray().Select(e => e.GetString()));
        Assert.Contains("ES512", doc.GetProperty("id_token_signing_alg_values_supported").EnumerateArray().Select(e => e.GetString()));
        Assert.Contains("offline_access", doc.GetProperty("scopes_supported").EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public async Task Jwks_publishes_public_key_only()
    {
        using var b = app.NewBrowser();
        var jwks = await b.Http.GetStringAsync("/.well-known/openid-configuration/jwks", Ct);
        var key = JsonDocument.Parse(jwks).RootElement.GetProperty("keys").EnumerateArray()
            .First(k => k.GetProperty("crv").GetString() == "P-521");
        Assert.Equal("EC", key.GetProperty("kty").GetString());
        Assert.Equal("sig", key.GetProperty("use").GetString());
        Assert.False(key.TryGetProperty("d", out _));
    }

    [Fact]
    public async Task Cors_allows_registered_origin_on_token_endpoint_only()
    {
        using var b = app.NewBrowser();
        async Task<HttpResponseMessage> Preflight(string path, string origin)
        {
            using var request = new HttpRequestMessage(HttpMethod.Options, path);
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "POST");
            return await b.Http.SendAsync(request, Ct);
        }

        var ok = await Preflight("/connect/token", IdServerFactory.SpaOrigin);
        Assert.Equal(IdServerFactory.SpaOrigin, ok.Headers.GetValues("Access-Control-Allow-Origin").Single());

        var unknown = await Preflight("/connect/token", "https://evil.test");
        Assert.False(unknown.Headers.Contains("Access-Control-Allow-Origin"));

        var otherPath = await Preflight("/connect/authorize", IdServerFactory.SpaOrigin);
        Assert.False(otherPath.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task End_session_signs_out_and_returns_to_registered_uri_with_state()
    {
        using var b = app.NewBrowser();
        var (p, verifier) = Browser.SpaRequest();
        var code = await b.GetCodeAsync(p);
        var (_, tokens) = await b.TokenAsync(new()
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = IdServerFactory.Spa,
            ["code"] = code,
            ["redirect_uri"] = IdServerFactory.SpaRedirect,
            ["code_verifier"] = verifier,
        });

        var r = await b.Http.GetAsync("/connect/endsession?id_token_hint=" + tokens.GetProperty("id_token").GetString()
            + "&post_logout_redirect_uri=" + Uri.EscapeDataString(IdServerFactory.SpaPostLogout) + "&state=bye", Ct);
        Assert.Equal(HttpStatusCode.Redirect, r.StatusCode);
        Assert.Equal(IdServerFactory.SpaPostLogout + "?state=bye", r.Headers.Location!.AbsoluteUri);

        // Session is gone: authorize asks for login again.
        var again = await b.AuthorizeAsync(Browser.SpaRequest().Parameters);
        Assert.Equal("/Identity/Account/Login", Browser.Path(again.Headers.Location!));
    }

    [Fact]
    public async Task End_session_refuses_unregistered_post_logout_uri()
    {
        using var b = app.NewBrowser();
        var r = await b.Http.GetAsync("/connect/endsession?client_id=spa&post_logout_redirect_uri="
            + Uri.EscapeDataString("https://evil.test/"), Ct);
        Assert.Equal("/Error", Browser.Path(r.Headers.Location!));
    }
}
