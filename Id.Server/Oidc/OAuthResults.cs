namespace Id.Server.Oidc;

/// <summary>Error codes from RFC 6749 / OIDC Core / RFC 6750.</summary>
public static class OAuthErrors
{
    public const string InvalidRequest = "invalid_request";
    public const string InvalidClient = "invalid_client";
    public const string InvalidGrant = "invalid_grant";
    public const string UnauthorizedClient = "unauthorized_client";
    public const string UnsupportedGrantType = "unsupported_grant_type";
    public const string UnsupportedResponseType = "unsupported_response_type";
    public const string InvalidScope = "invalid_scope";
    public const string LoginRequired = "login_required";
    public const string RequestNotSupported = "request_not_supported";
    public const string RequestUriNotSupported = "request_uri_not_supported";
    public const string InvalidToken = "invalid_token";
    public const string InsufficientScope = "insufficient_scope";
    public const string ServerError = "server_error";
}

public static class OAuthResults
{
    /// <summary>Token responses and errors must never be cached (RFC 6749 5.1).</summary>
    public static void NoStore(HttpResponse response)
    {
        response.Headers.CacheControl = "no-store";
        response.Headers.Pragma = "no-cache";
    }

    public static IResult Json(HttpContext context, object body, int status = StatusCodes.Status200OK)
    {
        NoStore(context.Response);
        return Results.Json(body, statusCode: status);
    }

    public static IResult Error(HttpContext context, string error, string? description = null, int status = StatusCodes.Status400BadRequest)
    {
        var body = new Dictionary<string, string> { ["error"] = error };
        if (description is not null) body["error_description"] = description;
        return Json(context, body, status);
    }
}
