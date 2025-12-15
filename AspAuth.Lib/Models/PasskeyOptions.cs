using Microsoft.AspNetCore.Identity;

namespace AspAuth.Lib.Models;

public class PasskeyUser
{
    public required string Id {get;set;}
    public required string Name {get;set;}
    public required string DisplayName {get;set;}
}

public class PassKeyViewModel
{
    public required string Name {get;set;}
}