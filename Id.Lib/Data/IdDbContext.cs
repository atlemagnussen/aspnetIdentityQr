using Id.Lib.Entities;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Id.Lib.Data;

/// <summary>
/// The one context for the whole auth database: ASP.NET Identity, OAuth/OIDC configuration (clients,
/// scopes, resources), operational grants, signing keys and the Data Protection key ring.
/// The schema predates this context (it was created by Identity + Duende IdentityServer migrations),
/// so every mapping here must produce exactly the existing tables. Migrations live in Id.Lib/Migrations.
/// </summary>
public class IdDbContext(DbContextOptions<IdDbContext> options)
    : IdentityDbContext<ApplicationUser>(options), IDataProtectionKeyContext
{

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<CryptoSigningKey> SigningKeys => Set<CryptoSigningKey>();
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<IdentityResource> IdentityResources => Set<IdentityResource>();
    public DbSet<ApiScope> ApiScopes => Set<ApiScope>();
    public DbSet<ApiResource> ApiResources => Set<ApiResource>();
    public DbSet<IdentityProvider> IdentityProviders => Set<IdentityProvider>();

    public DbSet<PersistedGrant> PersistedGrants => Set<PersistedGrant>();
    public DbSet<DeviceFlowCode> DeviceCodes => Set<DeviceFlowCode>();
    public DbSet<DuendeKey> Keys => Set<DuendeKey>();
    public DbSet<ServerSideSession> ServerSideSessions => Set<ServerSideSession>();
    public DbSet<PushedAuthorizationRequest> PushedAuthorizationRequests => Set<PushedAuthorizationRequest>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Identity picks its schema version from IdentityOptions in the application service provider.
        // Without v3 the passkeys table silently drops out of the model; fail loudly instead.
        if (builder.Model.FindEntityType(typeof(IdentityUserPasskey<string>)) is null)
            throw new InvalidOperationException(
                "IdDbContext needs Identity schema v3. Configure IdentityOptions with DatabaseSetup.ConfigureIdentityOptions.");

        ConfigureUsers(builder);
        ConfigureClients(builder);
        ConfigureResources(builder);
        ConfigureOperational(builder);
    }

    private static void ConfigureUsers(ModelBuilder builder)
    {
        // The original schema was created with Identity UI's Stores.MaxLengthForKeys = 128.
        builder.Entity<IdentityUserLogin<string>>(e =>
        {
            e.Property(x => x.LoginProvider).HasMaxLength(128);
            e.Property(x => x.ProviderKey).HasMaxLength(128);
        });
        builder.Entity<IdentityUserToken<string>>(e =>
        {
            e.Property(x => x.LoginProvider).HasMaxLength(128);
            e.Property(x => x.Name).HasMaxLength(128);
        });

        builder.Entity<UserProfile>(e =>
        {
            e.ToTable("UserProfile");
            e.HasKey(x => x.UserId);
            e.HasOne(x => x.User).WithOne(u => u.UserProfile)
                .HasForeignKey<UserProfile>(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CryptoSigningKey>(e =>
        {
            e.ToTable("SigningKeys");
            e.Property(x => x.Key).HasColumnType("jsonb");
        });

        builder.Entity<DataProtectionKey>().ToTable("DataProtectionKeys");
    }

    private static void ConfigureClients(ModelBuilder builder)
    {
        builder.Entity<Client>(e =>
        {
            e.ToTable("Clients");
            e.Property(x => x.ClientId).HasMaxLength(200).IsRequired();
            e.Property(x => x.ProtocolType).HasMaxLength(200).IsRequired();
            e.Property(x => x.ClientName).HasMaxLength(200);
            e.Property(x => x.ClientUri).HasMaxLength(2000);
            e.Property(x => x.LogoUri).HasMaxLength(2000);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.FrontChannelLogoutUri).HasMaxLength(2000);
            e.Property(x => x.BackChannelLogoutUri).HasMaxLength(2000);
            e.Property(x => x.ClientClaimsPrefix).HasMaxLength(200);
            e.Property(x => x.PairWiseSubjectSalt).HasMaxLength(200);
            e.Property(x => x.UserCodeType).HasMaxLength(100);
            e.Property(x => x.AllowedIdentityTokenSigningAlgorithms).HasMaxLength(100);
            e.Property(x => x.InitiateLoginUri).HasMaxLength(2000);
            e.HasIndex(x => x.ClientId).IsUnique();
        });

        Child<ClientGrantType>(builder, "ClientGrantTypes", c => c.AllowedGrantTypes,
            e => { e.Property(x => x.GrantType).HasMaxLength(250).IsRequired(); e.HasIndex(x => new { x.ClientId, x.GrantType }).IsUnique(); });
        Child<ClientRedirectUri>(builder, "ClientRedirectUris", c => c.RedirectUris,
            e => { e.Property(x => x.RedirectUri).HasMaxLength(400).IsRequired(); e.HasIndex(x => new { x.ClientId, x.RedirectUri }).IsUnique(); });
        Child<ClientPostLogoutRedirectUri>(builder, "ClientPostLogoutRedirectUris", c => c.PostLogoutRedirectUris,
            e => { e.Property(x => x.PostLogoutRedirectUri).HasMaxLength(400).IsRequired(); e.HasIndex(x => new { x.ClientId, x.PostLogoutRedirectUri }).IsUnique(); });
        Child<ClientScope>(builder, "ClientScopes", c => c.AllowedScopes,
            e => { e.Property(x => x.Scope).HasMaxLength(200).IsRequired(); e.HasIndex(x => new { x.ClientId, x.Scope }).IsUnique(); });
        Child<ClientIdPRestriction>(builder, "ClientIdPRestrictions", c => c.IdentityProviderRestrictions,
            e => { e.Property(x => x.Provider).HasMaxLength(200).IsRequired(); e.HasIndex(x => new { x.ClientId, x.Provider }).IsUnique(); });
        Child<ClientCorsOrigin>(builder, "ClientCorsOrigins", c => c.AllowedCorsOrigins,
            e => { e.Property(x => x.Origin).HasMaxLength(150).IsRequired(); e.HasIndex(x => new { x.ClientId, x.Origin }).IsUnique(); });
        Child<ClientClaim>(builder, "ClientClaims", c => c.Claims, e =>
        {
            e.Property(x => x.Type).HasMaxLength(250).IsRequired();
            e.Property(x => x.Value).HasMaxLength(250).IsRequired();
            e.HasIndex(x => new { x.ClientId, x.Type, x.Value }).IsUnique();
        });
        Child<ClientProperty>(builder, "ClientProperties", c => c.Properties, e =>
        {
            e.Property(x => x.Key).HasMaxLength(250).IsRequired();
            e.Property(x => x.Value).HasMaxLength(2000).IsRequired();
            e.HasIndex(x => new { x.ClientId, x.Key }).IsUnique();
        });
        Child<ClientSecret>(builder, "ClientSecrets", c => c.ClientSecrets, e =>
        {
            e.Property(x => x.Value).HasMaxLength(4000).IsRequired();
            e.Property(x => x.Type).HasMaxLength(250).IsRequired();
            e.Property(x => x.Description).HasMaxLength(2000);
        });
    }

    private static void Child<T>(ModelBuilder builder, string table,
        System.Linq.Expressions.Expression<Func<Client, IEnumerable<T>?>> collection,
        Action<EntityTypeBuilder<T>> configure) where T : ClientChild
    {
        builder.Entity<T>(e =>
        {
            e.ToTable(table);
            e.HasOne(x => x.Client).WithMany(collection).HasForeignKey(x => x.ClientId).IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
            configure(e);
        });
    }

    private static void ConfigureResources(ModelBuilder builder)
    {
        builder.Entity<IdentityResource>(e =>
        {
            e.ToTable("IdentityResources");
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasMany(x => x.UserClaims).WithOne(x => x.IdentityResource).HasForeignKey(x => x.IdentityResourceId)
                .IsRequired().OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Properties).WithOne(x => x.IdentityResource).HasForeignKey(x => x.IdentityResourceId)
                .IsRequired().OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<IdentityResourceClaim>(e =>
        {
            e.ToTable("IdentityResourceClaims");
            e.Property(x => x.Type).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.IdentityResourceId, x.Type }).IsUnique();
        });
        builder.Entity<IdentityResourceProperty>(e =>
        {
            e.ToTable("IdentityResourceProperties");
            e.Property(x => x.Key).HasMaxLength(250).IsRequired();
            e.Property(x => x.Value).HasMaxLength(2000).IsRequired();
            e.HasIndex(x => new { x.IdentityResourceId, x.Key }).IsUnique();
        });

        builder.Entity<ApiScope>(e =>
        {
            e.ToTable("ApiScopes");
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasMany(x => x.UserClaims).WithOne(x => x.Scope).HasForeignKey(x => x.ScopeId)
                .IsRequired().OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Properties).WithOne(x => x.Scope).HasForeignKey(x => x.ScopeId)
                .IsRequired().OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<ApiScopeClaim>(e =>
        {
            e.ToTable("ApiScopeClaims");
            e.Property(x => x.Type).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.ScopeId, x.Type }).IsUnique();
        });
        builder.Entity<ApiScopeProperty>(e =>
        {
            e.ToTable("ApiScopeProperties");
            e.Property(x => x.Key).HasMaxLength(250).IsRequired();
            e.Property(x => x.Value).HasMaxLength(2000).IsRequired();
            e.HasIndex(x => new { x.ScopeId, x.Key }).IsUnique();
        });

        builder.Entity<ApiResource>(e =>
        {
            e.ToTable("ApiResources");
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.AllowedAccessTokenSigningAlgorithms).HasMaxLength(100);
            e.HasIndex(x => x.Name).IsUnique();
            e.HasMany(x => x.Secrets).WithOne(x => x.ApiResource).HasForeignKey(x => x.ApiResourceId)
                .IsRequired().OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Scopes).WithOne(x => x.ApiResource).HasForeignKey(x => x.ApiResourceId)
                .IsRequired().OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.UserClaims).WithOne(x => x.ApiResource).HasForeignKey(x => x.ApiResourceId)
                .IsRequired().OnDelete(DeleteBehavior.Cascade);
            e.HasMany(x => x.Properties).WithOne(x => x.ApiResource).HasForeignKey(x => x.ApiResourceId)
                .IsRequired().OnDelete(DeleteBehavior.Cascade);
        });
        builder.Entity<ApiResourceSecret>(e =>
        {
            e.ToTable("ApiResourceSecrets");
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Value).HasMaxLength(4000).IsRequired();
            e.Property(x => x.Type).HasMaxLength(250).IsRequired();
        });
        builder.Entity<ApiResourceScope>(e =>
        {
            e.ToTable("ApiResourceScopes");
            e.Property(x => x.Scope).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.ApiResourceId, x.Scope }).IsUnique();
        });
        builder.Entity<ApiResourceClaim>(e =>
        {
            e.ToTable("ApiResourceClaims");
            e.Property(x => x.Type).HasMaxLength(200).IsRequired();
            e.HasIndex(x => new { x.ApiResourceId, x.Type }).IsUnique();
        });
        builder.Entity<ApiResourceProperty>(e =>
        {
            e.ToTable("ApiResourceProperties");
            e.Property(x => x.Key).HasMaxLength(250).IsRequired();
            e.Property(x => x.Value).HasMaxLength(2000).IsRequired();
            e.HasIndex(x => new { x.ApiResourceId, x.Key }).IsUnique();
        });

        builder.Entity<IdentityProvider>(e =>
        {
            e.ToTable("IdentityProviders");
            e.Property(x => x.Scheme).HasMaxLength(200).IsRequired();
            e.Property(x => x.Type).HasMaxLength(20).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(200);
            e.HasIndex(x => x.Scheme).IsUnique();
        });
    }

    private static void ConfigureOperational(ModelBuilder builder)
    {
        builder.Entity<PersistedGrant>(e =>
        {
            e.ToTable("PersistedGrants");
            e.Property(x => x.Key).HasMaxLength(200);
            e.Property(x => x.Type).HasMaxLength(50).IsRequired();
            e.Property(x => x.SubjectId).HasMaxLength(200);
            e.Property(x => x.SessionId).HasMaxLength(100);
            e.Property(x => x.ClientId).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(200);
            e.Property(x => x.Data).HasMaxLength(50000).IsRequired();
            e.HasIndex(x => x.Key).IsUnique();
            e.HasIndex(x => new { x.SubjectId, x.ClientId, x.Type });
            e.HasIndex(x => new { x.SubjectId, x.SessionId, x.Type });
            e.HasIndex(x => x.Expiration);
            e.HasIndex(x => x.ConsumedTime);
        });

        builder.Entity<DeviceFlowCode>(e =>
        {
            e.ToTable("DeviceCodes");
            e.HasKey(x => x.UserCode);
            e.Property(x => x.UserCode).HasMaxLength(200);
            e.Property(x => x.DeviceCode).HasMaxLength(200).IsRequired();
            e.Property(x => x.SubjectId).HasMaxLength(200);
            e.Property(x => x.SessionId).HasMaxLength(100);
            e.Property(x => x.ClientId).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(200);
            e.Property(x => x.Data).HasMaxLength(50000).IsRequired();
            e.HasIndex(x => x.DeviceCode).IsUnique();
            e.HasIndex(x => x.Expiration);
        });

        builder.Entity<DuendeKey>(e =>
        {
            e.ToTable("Keys");
            e.Property(x => x.Algorithm).HasMaxLength(100).IsRequired();
            e.Property(x => x.Data).IsRequired();
            e.HasIndex(x => x.Use);
        });

        builder.Entity<ServerSideSession>(e =>
        {
            e.ToTable("ServerSideSessions");
            e.Property(x => x.Key).HasMaxLength(100).IsRequired();
            e.Property(x => x.Scheme).HasMaxLength(100).IsRequired();
            e.Property(x => x.SubjectId).HasMaxLength(100).IsRequired();
            e.Property(x => x.SessionId).HasMaxLength(100);
            e.Property(x => x.DisplayName).HasMaxLength(100);
            e.Property(x => x.Data).IsRequired();
            e.HasIndex(x => x.Key).IsUnique();
            e.HasIndex(x => x.SubjectId);
            e.HasIndex(x => x.SessionId);
            e.HasIndex(x => x.DisplayName);
            e.HasIndex(x => x.Expires);
        });

        builder.Entity<PushedAuthorizationRequest>(e =>
        {
            e.ToTable("PushedAuthorizationRequests");
            e.Property(x => x.ReferenceValueHash).HasMaxLength(64).IsRequired();
            e.Property(x => x.Parameters).IsRequired();
            e.HasIndex(x => x.ReferenceValueHash).IsUnique();
            e.HasIndex(x => x.ExpiresAtUtc);
        });
    }
}
