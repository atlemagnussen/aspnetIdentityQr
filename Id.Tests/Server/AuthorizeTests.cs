using System.Net;
using Id.Tests.Infrastructure;
using Microsoft.AspNetCore.WebUtilities;

namespace Id.Tests.Server;

public class AuthorizeTests(IdServerFactory app)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Unknown_client_goes_to_error_page_not_redirect_uri()
    {
        using var b = app.NewBrowser();
        var (p, _) = Browser.SpaRequest();
        p["client_id"] = "nope";
        var r = await b.AuthorizeAsync(p);
        Assert.Equal("/Error", Browser.Path(r.Headers.Location!));
    }

    [Fact]
    public async Task Unregistered_redirect_uri_goes_to_error_page()
    {
        using var b = app.NewBrowser();
        var (p, _) = Browser.SpaRequest();
        p["redirect_uri"] = "https://evil.test/cb";
        var r = await b.AuthorizeAsync(p);
        Assert.Equal("/Error", Browser.Path(r.Headers.Location!));
    }

    [Fact]
    public async Task Redirect_uri_must_match_exactly()
    {
        using var b = app.NewBrowser();
        var (p, _) = Browser.SpaRequest();
        p["redirect_uri"] = IdServerFactory.SpaRedirect + "/";
        var r = await b.AuthorizeAsync(p);
        Assert.Equal("/Error", Browser.Path(r.Headers.Location!));
    }

    [Fact]
    public async Task Repeated_parameter_is_rejected()
    {
        using var b = app.NewBrowser();
        var r = await b.Http.GetAsync("/connect/authorize?client_id=spa&client_id=conf", Ct);
        Assert.Equal("/Error", Browser.Path(r.Headers.Location!));
    }

    [Fact]
    public async Task Anonymous_user_is_sent_to_login_with_return_to_authorize()
    {
        using var b = app.NewBrowser();
        var (p, _) = Browser.SpaRequest();
        var r = await b.AuthorizeAsync(p);
        Assert.Equal("/Identity/Account/Login", Browser.Path(r.Headers.Location!));
        var returnUrl = Browser.LocationQuery(r.Headers.Location!)["ReturnUrl"];
        Assert.StartsWith("/connect/authorize?", returnUrl);
    }

    [Fact]
    public async Task Prompt_none_without_session_returns_login_required_to_client()
    {
        using var b = app.NewBrowser();
        var (p, _) = Browser.SpaRequest();
        p["prompt"] = "none";
        var q = Browser.Query(await b.AuthorizeAsync(p));
        Assert.Equal("login_required", q["error"]);
        Assert.Equal("st-123", q["state"]);
        Assert.Equal(IdServerFactory.Issuer, q["iss"]);
    }

    [Theory]
    [InlineData("code_challenge", null, "invalid_request")]          // PKCE required for this client
    [InlineData("code_challenge_method", "plain", "invalid_request")] // plain never accepted
    [InlineData("code_challenge", "short", "invalid_request")]
    [InlineData("response_type", "token", "unsupported_response_type")]
    [InlineData("response_type", "code id_token", "unsupported_response_type")]
    [InlineData("scope", "openid unknown", "invalid_scope")]
    [InlineData("scope", "openid offline_access email", "invalid_scope")] // email not allowed for client
    [InlineData("request", "eyJhbGciOiJub25lIn0.e30.", "request_not_supported")]
    [InlineData("response_mode", "fragment", "invalid_request")]
    public async Task Invalid_requests_return_error_to_client(string parameter, string? value, string error)
    {
        using var b = app.NewBrowser();
        await b.LoginAsync();
        var (p, _) = Browser.SpaRequest();
        p[parameter] = value;
        if (value is null) p.Remove(parameter);
        var q = Browser.Query(await b.AuthorizeAsync(p));
        Assert.Equal(error, q["error"]);
        Assert.Equal("st-123", q["state"]);
        Assert.False(q.ContainsKey("code"));
    }

    [Fact]
    public async Task Logged_in_user_gets_code_with_state_and_iss()
    {
        using var b = app.NewBrowser();
        await b.LoginAsync();
        var (p, _) = Browser.SpaRequest();
        var r = await b.AuthorizeAsync(p);
        Assert.StartsWith(IdServerFactory.SpaRedirect + "?", r.Headers.Location!.AbsoluteUri);
        var q = Browser.Query(r);
        Assert.Equal(43, q["code"].Length);
        Assert.Equal("st-123", q["state"]);
        Assert.Equal(IdServerFactory.Issuer, q["iss"]);
    }

    [Fact]
    public async Task Prompt_login_forces_login_even_with_session()
    {
        using var b = app.NewBrowser();
        await b.LoginAsync();
        var (p, _) = Browser.SpaRequest();
        p["prompt"] = "login";
        var r = await b.AuthorizeAsync(p);
        Assert.Equal("/Identity/Account/Login", Browser.Path(r.Headers.Location!));
        Assert.DoesNotContain("prompt", Browser.LocationQuery(r.Headers.Location!)["ReturnUrl"]);
    }

    [Fact]
    public async Task Form_post_returns_auto_submitting_form()
    {
        using var b = app.NewBrowser();
        await b.LoginAsync();
        var (p, _) = Browser.SpaRequest();
        p["response_mode"] = "form_post";
        var r = await b.AuthorizeAsync(p);
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        var html = await r.Content.ReadAsStringAsync(Ct);
        Assert.Contains($"action=\"{IdServerFactory.SpaRedirect}\"", html);
        Assert.Contains("name=\"code\"", html);
        Assert.Equal("no-store", r.Headers.CacheControl!.ToString());
    }

    [Fact]
    public async Task Login_page_ignores_external_return_url()
    {
        // Open redirect fix: the old login page redirected to any ReturnUrl after sign-in.
        using var b = app.NewBrowser();
        var target = await b.LoginAsync("https://evil.test/");
        Assert.Equal("/", target);
    }
}
