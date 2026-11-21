using Id.Lib.Oidc;
using Id.Lib.Services;

namespace Id.Tests.Lib;

public class HashingAndGrantTests
{
    [Fact]
    public void Sha256_matches_Duende_format()
    {
        // Duende's well-known hash of "secret". Existing ClientSecrets rows use this format.
        Assert.Equal("K7gNU3sdo+OL0wNhqoVWhr3g6s1xYv72ol/pe/Unols=", "secret".Sha256());
        Assert.Equal("", "".Sha256());
        Assert.Equal("", ((string?)null).Sha256());
    }

    [Theory]
    [InlineData("authorization_code")]
    [InlineData("authorization_code", "client_credentials")]
    [InlineData("client_credentials")]
    [InlineData("implicit", "client_credentials")]
    public void Valid_grant_combinations(params string[] grants) => GrantTypes.Validate(grants);

    [Theory]
    [InlineData("implicit", "authorization_code")]
    [InlineData("hybrid", "authorization_code")]
    [InlineData("implicit", "hybrid")]
    [InlineData("authorization_code", "authorization_code")]
    [InlineData("refresh_token")]
    [InlineData("has space")]
    public void Invalid_grant_combinations(params string[] grants) =>
        Assert.Throws<InvalidOperationException>(() => GrantTypes.Validate(grants));
}
