namespace UserAdmin.Auth;

/// <param name="AuthServer">Base URL of the ID server (env AUTH_SERVER)</param>
/// <param name="AuthClient">Client id UserAdmin itself signs in with (env AUTH_CLIENT). Protected from delete/disable.</param>
public sealed record UserAdminOptions(string AuthServer, string AuthClient);
