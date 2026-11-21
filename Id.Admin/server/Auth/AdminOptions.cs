namespace Id.Admin.Auth;

/// <param name="AuthServer">Base URL of the ID server (env AUTH_SERVER)</param>
/// <param name="AuthClient">Client id the admin app itself signs in with (env AUTH_CLIENT). Protected from delete/disable.</param>
public sealed record AdminOptions(string AuthServer, string AuthClient);
