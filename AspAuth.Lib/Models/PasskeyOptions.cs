namespace AspAuth.Lib.Models;

public class PasskeyUser
{
    public required string Id {get;set;}
    public required string Name {get;set;}
    public required string DisplayName {get;set;}
}

public class PassKeyViewModel
{
    /// <summary>
    /// Base64 key
    /// </summary>
    public required string CredentialId {get;set;}
    public required string Name {get;set;}
}