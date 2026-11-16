using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Catalog.Api.Infrastructure.Auth;

public static class AuthServiceCollectionExtensions
{
    public const string PlatformAdminPolicy = "PlatformAdmin";
    public const string AdminPolicy = "Admin";

    public static IServiceCollection AddCatalogAuth(this IServiceCollection services)
    {
        services.AddOptions<AuthOptions>().BindConfiguration(AuthOptions.SectionName);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configured lazily from AuthOptions so tests can override the settings.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<AuthOptions>>((jwt, authOptions) => Configure(jwt, authOptions.Value));

        services.AddSingleton<IAuthorizationHandler, PlatformAdminHandler>();
        services.AddScoped<IAuthorizationHandler, AdminHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, CatalogAuthorizationResultHandler>();
        services.AddAuthorizationBuilder()
            .AddPolicy(PlatformAdminPolicy, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PlatformAdminRequirement()))
            .AddPolicy(AdminPolicy, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new AdminRequirement()));

        return services;
    }

    private static void Configure(JwtBearerOptions jwt, AuthOptions auth)
    {
        jwt.MapInboundClaims = false;
        jwt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = !string.IsNullOrWhiteSpace(auth.Issuer),
            ValidIssuer = auth.Issuer,
            ValidateAudience = true,
            ValidAudience = auth.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimNames.Email,
        };

        if (!string.IsNullOrWhiteSpace(auth.JwtSecret))
        {
            jwt.TokenValidationParameters.IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(auth.JwtSecret));
            jwt.TokenValidationParameters.ValidAlgorithms = [SecurityAlgorithms.HmacSha256];
        }
        else if (!string.IsNullOrWhiteSpace(auth.JwksUrl))
        {
            jwt.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                auth.JwksUrl, new JwksRetriever(), new HttpDocumentRetriever { RequireHttps = true });
        }
    }

    /// <summary>Reads a bare JWKS document (Supabase does not need a full OIDC discovery document).</summary>
    private sealed class JwksRetriever : IConfigurationRetriever<OpenIdConnectConfiguration>
    {
        public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(
            string address, IDocumentRetriever retriever, CancellationToken cancel)
        {
            var document = await retriever.GetDocumentAsync(address, cancel);
            var keySet = new JsonWebKeySet(document);
            var configuration = new OpenIdConnectConfiguration();
            foreach (var key in keySet.GetSigningKeys())
            {
                configuration.SigningKeys.Add(key);
            }

            return configuration;
        }
    }
}
