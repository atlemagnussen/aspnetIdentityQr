using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Id.Server.Oidc.Tokens;
using Id.Tests.Infrastructure;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Id.Tests.Server;

public class TokenFlowTests(IdServerFactory app)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static Dictionary<string, string> CodeForm(string code, string verifier, string clientId = IdServerFactory.Spa,
        string redirect = IdServerFactory.SpaRedirect) => new()
    {
        ["grant_type"] = "authorization_code",
        ["client_id"] = clientId,
        ["code"] = code,
        ["redirect_uri"] = redirect,
        ["code_verifier"] = verifier,
    };

    [Fact]
    public async Task Full_flow_issues_valid_tokens_with_the_old_server_shape()
    {
        using var b = app.NewBrowser();
        var (p, verifier) = Browser.SpaRequest();
        var code = await b.GetCodeAsync(p);

        var (status, body) = await b.TokenAsync(CodeForm(code, verifier));
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("Bearer", body.GetProperty("token_type").GetString());
        Assert.Equal(3600, body.GetProperty("expires_in").GetInt32());
        Assert.False(body.TryGetProperty("refresh_token", out _)); // no offline_access requested

        var keys = await JwksAsync(b);
        var handler = new JsonWebTokenHandler();

        // Access token
        var accessToken = body.GetProperty("access_token").GetString()!;
        var at = await handler.ValidateTokenAsync(accessToken, new TokenValidationParameters
        {
            ValidIssuer = IdServerFactory.Issuer,
            ValidAudiences = [$"{IdServerFactory.Issuer}/resources", IdServerFactory.ApiResource],
            IssuerSigningKeys = keys,
        });
        Assert.True(at.IsValid, at.Exception?.Message);
        var atJwt = (JsonWebToken)at.SecurityToken;
        Assert.Equal("at+jwt", atJwt.Typ);
        Assert.Equal("ES512", atJwt.Alg);
        Assert.False(atJwt.TryGetHeaderValue<string>("kid", out _)); // same as production
        Assert.Equal(app.UserId, atJwt.Subject);
        Assert.Equal(IdServerFactory.Spa, atJwt.GetClaim("client_id").Value);
        Assert.Equal(["openid", "profile", "roles", "api1"], atJwt.GetPayloadValue<string[]>("scope"));
        Assert.Contains($"{IdServerFactory.Issuer}/resources", atJwt.Audiences);
        Assert.Contains(IdServerFactory.ApiResource, atJwt.Audiences);
        Assert.Equal("local", atJwt.GetClaim("idp").Value);
        Assert.Equal("pwd", atJwt.GetClaim("amr").Value);
        Assert.Equal("Admin", atJwt.GetClaim("role").Value);                   // api1 scope asks for role
        Assert.Equal(IdServerFactory.UserName, atJwt.GetClaim("email").Value); // apires asks for email
        Assert.True(atJwt.TryGetClaim("sid", out _));
        Assert.True(atJwt.TryGetClaim("jti", out _));
        Assert.False(atJwt.TryGetClaim("fullname", out _));                    // not requested by any API

        // Id token
        var idToken = body.GetProperty("id_token").GetString()!;
        var it = await handler.ValidateTokenAsync(idToken, new TokenValidationParameters
        {
            ValidIssuer = IdServerFactory.Issuer,
            ValidAudience = IdServerFactory.Spa,
            IssuerSigningKeys = keys,
        });
        Assert.True(it.IsValid, it.Exception?.Message);
        var idJwt = (JsonWebToken)it.SecurityToken;
        Assert.Equal("n-456", idJwt.GetClaim("nonce").Value);
        Assert.Equal(TokenIssuer.LeftHalfHash(accessToken, "ES512"), idJwt.GetClaim("at_hash").Value);
        Assert.Equal(atJwt.GetClaim("sid").Value, idJwt.GetClaim("sid").Value);
        Assert.False(idJwt.TryGetClaim("role", out _)); // user claims come from userinfo

        // Userinfo
        using var request = new HttpRequestMessage(HttpMethod.Get, "/connect/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var info = await b.Http.SendAsync(request, Ct);
        Assert.Equal(HttpStatusCode.OK, info.StatusCode);
        var claims = JsonDocument.Parse(await info.Content.ReadAsStringAsync(Ct)).RootElement;
        Assert.Equal(app.UserId, claims.GetProperty("sub").GetString());
        Assert.Equal(IdServerFactory.UserName, claims.GetProperty("name").GetString());
        Assert.Equal("Admin", claims.GetProperty("role").GetString());
        Assert.Equal(IdServerFactory.FullName, claims.GetProperty("fullname").GetString());
        Assert.False(claims.TryGetProperty("email", out _)); // no identity resource asked for it
    }

    [Fact]
    public async Task Code_is_single_use()
    {
        using var b = app.NewBrowser();
        var (p, verifier) = Browser.SpaRequest();
        var code = await b.GetCodeAsync(p);
        Assert.Equal(HttpStatusCode.OK, (await b.TokenAsync(CodeForm(code, verifier))).Status);

        var (status, body) = await b.TokenAsync(CodeForm(code, verifier));
        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.Equal("invalid_grant", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Wrong_code_verifier_is_rejected_and_burns_the_code()
    {
        using var b = app.NewBrowser();
        var (p, verifier) = Browser.SpaRequest();
        var code = await b.GetCodeAsync(p);

        var wrong = Browser.SpaRequest().Verifier;
        Assert.Equal("invalid_grant", (await b.TokenAsync(CodeForm(code, wrong))).Body.GetProperty("error").GetString());
        Assert.Equal("invalid_grant", (await b.TokenAsync(CodeForm(code, verifier))).Body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Missing_code_verifier_is_rejected()
    {
        using var b = app.NewBrowser();
        var (p, _) = Browser.SpaRequest();
        var code = await b.GetCodeAsync(p);
        var form = CodeForm(code, "");
        form.Remove("code_verifier");
        Assert.Equal("invalid_grant", (await b.TokenAsync(form)).Body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Redirect_uri_must_match_the_authorize_request()
    {
        using var b = app.NewBrowser();
        var (p, verifier) = Browser.SpaRequest();
        var code = await b.GetCodeAsync(p);
        var (_, body) = await b.TokenAsync(CodeForm(code, verifier, redirect: "https://app.test/other"));
        Assert.Equal("invalid_grant", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Code_is_bound_to_its_client()
    {
        using var b = app.NewBrowser();
        var (p, verifier) = Browser.SpaRequest();
        var code = await b.GetCodeAsync(p);
        var form = CodeForm(code, verifier, IdServerFactory.Confidential);
        var (_, body) = await b.TokenAsync(form, Basic(IdServerFactory.Confidential, IdServerFactory.ConfidentialSecret));
        Assert.Equal("invalid_grant", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Confidential_client_needs_its_secret()
    {
        using var b = app.NewBrowser();
        var code = await b.GetCodeAsync(ConfidentialRequest());
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = IdServerFactory.ConfidentialRedirect,
        };

        var (status, body) = await b.TokenAsync(form, Basic(IdServerFactory.Confidential, "wrong"));
        Assert.Equal(HttpStatusCode.Unauthorized, status);
        Assert.Equal("invalid_client", body.GetProperty("error").GetString());

        (status, body) = await b.TokenAsync(form, Basic(IdServerFactory.Confidential, IdServerFactory.ConfidentialSecret));
        Assert.Equal(HttpStatusCode.OK, status);
        Assert.True(body.TryGetProperty("id_token", out _));
    }

    [Fact]
    public async Task Client_secret_post_works()
    {
        using var b = app.NewBrowser();
        var code = await b.GetCodeAsync(ConfidentialRequest());
        var (status, _) = await b.TokenAsync(new()
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = IdServerFactory.ConfidentialRedirect,
            ["client_id"] = IdServerFactory.Confidential,
            ["client_secret"] = IdServerFactory.ConfidentialSecret,
        });
        Assert.Equal(HttpStatusCode.OK, status);
    }

    [Fact]
    public async Task Verifier_without_challenge_is_a_downgrade_and_rejected()
    {
        using var b = app.NewBrowser();
        var code = await b.GetCodeAsync(ConfidentialRequest());
        var (_, body) = await b.TokenAsync(new()
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = IdServerFactory.ConfidentialRedirect,
            ["code_verifier"] = Browser.SpaRequest().Verifier,
        }, Basic(IdServerFactory.Confidential, IdServerFactory.ConfidentialSecret));
        Assert.Equal("invalid_grant", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Refresh_token_rotates_and_old_one_stops_working()
    {
        using var b = app.NewBrowser();
        var (p, verifier) = Browser.SpaRequest("openid roles offline_access");
        var code = await b.GetCodeAsync(p);
        var (_, first) = await b.TokenAsync(CodeForm(code, verifier));
        var refresh1 = first.GetProperty("refresh_token").GetString()!;

        Dictionary<string, string> RefreshForm(string token) => new()
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = IdServerFactory.Spa,
            ["refresh_token"] = token,
        };

        var (status, second) = await b.TokenAsync(RefreshForm(refresh1));
        Assert.Equal(HttpStatusCode.OK, status);
        var refresh2 = second.GetProperty("refresh_token").GetString()!;
        Assert.NotEqual(refresh1, refresh2);
        Assert.True(second.TryGetProperty("id_token", out _));

        Assert.Equal("invalid_grant", (await b.TokenAsync(RefreshForm(refresh1))).Body.GetProperty("error").GetString());
        Assert.Equal(HttpStatusCode.OK, (await b.TokenAsync(RefreshForm(refresh2))).Status);
    }

    [Fact]
    public async Task Refresh_cannot_widen_scope()
    {
        using var b = app.NewBrowser();
        var (p, verifier) = Browser.SpaRequest("openid offline_access");
        var code = await b.GetCodeAsync(p);
        var (_, first) = await b.TokenAsync(CodeForm(code, verifier));
        var (_, body) = await b.TokenAsync(new()
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = IdServerFactory.Spa,
            ["refresh_token"] = first.GetProperty("refresh_token").GetString()!,
            ["scope"] = "openid api1",
        });
        Assert.Equal("invalid_scope", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Unsupported_grant_type()
    {
        using var b = app.NewBrowser();
        var (_, body) = await b.TokenAsync(new() { ["grant_type"] = "password", ["client_id"] = IdServerFactory.Spa });
        Assert.Equal("unsupported_grant_type", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Userinfo_rejects_missing_and_forged_tokens()
    {
        using var b = app.NewBrowser();
        var none = await b.Http.GetAsync("/connect/userinfo", Ct);
        Assert.Equal(HttpStatusCode.Unauthorized, none.StatusCode);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/connect/userinfo");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJub25lIiwidHlwIjoiYXQrand0In0.eyJzdWIiOiJ4Iiwic2NvcGUiOiJvcGVuaWQifQ.");
        var forged = await b.Http.SendAsync(request, Ct);
        Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);
        Assert.Contains("invalid_token", forged.Headers.WwwAuthenticate.ToString());
    }

    private static Dictionary<string, string?> ConfidentialRequest() => new()
    {
        ["client_id"] = IdServerFactory.Confidential,
        ["redirect_uri"] = IdServerFactory.ConfidentialRedirect,
        ["response_type"] = "code",
        ["scope"] = "openid profile",
        ["state"] = "s",
    };

    private static AuthenticationHeaderValue Basic(string id, string secret) =>
        new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Uri.EscapeDataString(id)}:{Uri.EscapeDataString(secret)}")));

    private static async Task<IList<JsonWebKey>> JwksAsync(Browser b) =>
        new JsonWebKeySet(await b.Http.GetStringAsync("/.well-known/openid-configuration/jwks", Ct)).Keys;
}
