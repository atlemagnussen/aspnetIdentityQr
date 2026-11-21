using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

namespace Id.Tests.Infrastructure;

/// <summary>A cookie-keeping client that doesn't follow redirects, plus protocol helpers.</summary>
public sealed partial class Browser(HttpClient http) : IDisposable
{
    public HttpClient Http => http;
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [GeneratedRegex("name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryToken();

    /// <summary>Signs in through the real login page. Returns the redirect target.</summary>
    public async Task<string> LoginAsync(string returnUrl = "/", string? password = null)
    {
        var page = await http.GetStringAsync($"/Identity/Account/Login?ReturnUrl={Uri.EscapeDataString(returnUrl)}", Ct);
        var token = AntiforgeryToken().Match(page).Groups[1].Value;
        var response = await http.PostAsync($"/Identity/Account/Login?ReturnUrl={Uri.EscapeDataString(returnUrl)}",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["Input.Email"] = IdServerFactory.UserName,
                ["Input.Password"] = password ?? IdServerFactory.Password,
                ["__RequestVerificationToken"] = token,
            }), Ct);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return response.Headers.Location!.OriginalString;
    }

    public async Task<HttpResponseMessage> AuthorizeAsync(Dictionary<string, string?> parameters) =>
        await http.GetAsync(QueryHelpers.AddQueryString("/connect/authorize", parameters), Ct);

    /// <summary>Default SPA authorize request with PKCE. Returns the parameters and the verifier.</summary>
    public static (Dictionary<string, string?> Parameters, string Verifier) SpaRequest(string scope = "openid profile roles api1")
    {
        var verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        return (new Dictionary<string, string?>
        {
            ["client_id"] = IdServerFactory.Spa,
            ["redirect_uri"] = IdServerFactory.SpaRedirect,
            ["response_type"] = "code",
            ["scope"] = scope,
            ["state"] = "st-123",
            ["nonce"] = "n-456",
            ["code_challenge"] = Challenge(verifier),
            ["code_challenge_method"] = "S256",
        }, verifier);
    }

    public static string Challenge(string verifier) =>
        WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

    /// <summary>Logs in (if needed) and runs authorize to the client redirect. Returns the code.</summary>
    public async Task<string> GetCodeAsync(Dictionary<string, string?> parameters)
    {
        var response = await AuthorizeAsync(parameters);
        if (Path(response.Headers.Location!) == "/Identity/Account/Login")
        {
            var returnUrl = LocationQuery(response.Headers.Location!)["ReturnUrl"];
            var back = await LoginAsync(returnUrl);
            response = await http.GetAsync(back, Ct);
        }
        var query = Query(response);
        Assert.False(query.ContainsKey("error"), query.GetValueOrDefault("error_description"));
        return query["code"];
    }

    public async Task<(HttpStatusCode Status, JsonElement Body)> TokenAsync(Dictionary<string, string> form, AuthenticationHeaderValue? auth = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/connect/token") { Content = new FormUrlEncodedContent(form) };
        request.Headers.Authorization = auth;
        var response = await http.SendAsync(request, Ct);
        var body = await response.Content.ReadAsStringAsync(Ct);
        return (response.StatusCode, JsonDocument.Parse(body.Length == 0 ? "{}" : body).RootElement.Clone());
    }

    public static string Path(Uri location) =>
        location.IsAbsoluteUri ? location.AbsolutePath : location.OriginalString.Split('?')[0];

    /// <summary>Query of a redirect.</summary>
    public static Dictionary<string, string> Query(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        return LocationQuery(response.Headers.Location!);
    }

    /// <summary>Query of a relative or absolute location.</summary>
    public static Dictionary<string, string> LocationQuery(Uri location) =>
        QueryHelpers.ParseQuery(location.IsAbsoluteUri ? location.Query : "?" + location.OriginalString.Split('?', 2).ElementAtOrDefault(1))
            .ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

    public void Dispose() => http.Dispose();
}
