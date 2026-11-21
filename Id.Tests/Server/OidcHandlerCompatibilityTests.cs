using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Id.Tests.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing.Handlers;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Id.Tests.Server;

/// <summary>
/// Microsoft's own OIDC client handler, set up like Id.Admin, signs in against Id.Server.
/// This is the drop-in check: what works here works for the existing ASP.NET clients.
/// </summary>
public partial class OidcHandlerCompatibilityTests(IdServerFactory app)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [GeneratedRegex("<input type=\"hidden\" name=\"([^\"]+)\" value=\"([^\"]*)\">")]
    private static partial Regex HiddenInput();

    [Fact]
    public async Task AspNet_oidc_handler_signs_in_with_roles_from_userinfo()
    {
        await using var rp = await StartRelyingPartyAsync();
        // TestServer handlers don't follow redirects; the cookie handler keeps the RP's correlation/nonce cookies.
        using var rpBrowser = new Browser(new HttpClient(new CookieContainerHandler { InnerHandler = rp.GetTestServer().CreateHandler() })
        {
            BaseAddress = new Uri(IdServerFactory.RpBase),
        });
        using var idBrowser = app.NewBrowser();

        // 1. Protected page on the RP challenges to the ID server.
        var challenge = await rpBrowser.Http.GetAsync("/me", Ct);
        Assert.Equal(HttpStatusCode.Redirect, challenge.StatusCode);
        var authorize = challenge.Headers.Location!;
        Assert.Equal($"{IdServerFactory.Issuer}/connect/authorize", authorize.GetLeftPart(UriPartial.Path));

        // 2. ID server: login, then back to authorize, which answers with form_post (the handler's default).
        var toLogin = await idBrowser.Http.GetAsync(authorize.PathAndQuery, Ct);
        var back = await idBrowser.LoginAsync(Browser.LocationQuery(toLogin.Headers.Location!)["ReturnUrl"]);
        var formPost = await idBrowser.Http.GetAsync(back, Ct);
        Assert.Equal(HttpStatusCode.OK, formPost.StatusCode);
        var html = await formPost.Content.ReadAsStringAsync(Ct);
        var fields = HiddenInput().Matches(html).ToDictionary(m => m.Groups[1].Value, m => WebUtility.HtmlDecode(m.Groups[2].Value));

        // 3. RP callback: code exchange, id token validation, userinfo, all through the real handler.
        var callback = await rpBrowser.Http.PostAsync("/signin-oidc", new FormUrlEncodedContent(fields), Ct);
        Assert.True(callback.StatusCode == HttpStatusCode.Redirect, await callback.Content.ReadAsStringAsync(Ct));
        Assert.Equal("/me", callback.Headers.Location!.OriginalString);

        var me = JsonDocument.Parse(await rpBrowser.Http.GetStringAsync("/me", Ct)).RootElement;
        Assert.Equal(app.UserId, me.GetProperty("sub").GetString());
        Assert.Equal(IdServerFactory.UserName, me.GetProperty("name").GetString());
        Assert.Equal("Admin", me.GetProperty("role").GetString());
    }

    /// <summary>A minimal RP with Id.Admin's OIDC settings; its back channel talks to the in-memory ID server.</summary>
    private async Task<WebApplication> StartRelyingPartyAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthentication(o =>
            {
                o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = "oidc";
            })
            .AddCookie()
            .AddOpenIdConnect("oidc", o =>
            {
                o.Authority = IdServerFactory.Issuer;
                o.ClientId = IdServerFactory.Rp;
                o.ResponseType = "code";
                o.SaveTokens = true;
                o.GetClaimsFromUserInfoEndpoint = true;
                o.Scope.Add("openid");
                o.Scope.Add("profile");
                o.Scope.Add("roles");
                o.MapInboundClaims = false;
                o.ClaimActions.Clear();
                o.ClaimActions.DeleteClaim("nonce");
                o.ClaimActions.DeleteClaim("at_hash");
                o.ClaimActions.MapJsonKey("role", "role", "role");
                o.ClaimActions.MapUniqueJsonKey("name", "name", "name");
                o.TokenValidationParameters.NameClaimType = "name";
                o.TokenValidationParameters.RoleClaimType = "role";
                o.BackchannelHttpHandler = app.Server.CreateHandler();
            });
        builder.Services.AddAuthorization();

        var rp = builder.Build();
        rp.UseAuthentication();
        rp.UseAuthorization();
        rp.MapGet("/me", (HttpContext http) => Results.Json(new
        {
            sub = http.User.FindFirst("sub")?.Value,
            name = http.User.Identity?.Name,
            role = http.User.FindFirst("role")?.Value,
        })).RequireAuthorization();
        await rp.StartAsync(Ct);
        return rp;
    }
}

