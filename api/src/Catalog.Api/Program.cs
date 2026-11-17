using System.Text.Json.Serialization;
using Catalog.Api.Data;
using Catalog.Api.Features.Health;
using Catalog.Api.Features.Me;
using Catalog.Api.Infrastructure;
using Catalog.Api.Infrastructure.Auth;
using Catalog.Api.Infrastructure.Identity;
using Catalog.Api.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    // Numbers are JSON numbers only, so the OpenAPI document (and the web client) types them as numbers.
    options.SerializerOptions.NumberHandling = JsonNumberHandling.Strict;
});
builder.Services.AddOpenApi(options =>
{
    OpenApiSchemaNames.Configure(options);
    OpenApiRouteParameters.Configure(options);
    OpenApiJsonValues.Configure(options);
});
builder.Services.AddCatalogData();
builder.Services.AddHealthChecks().AddDbContextCheck<CatalogDbContext>("database");
builder.Services.AddCatalogAuth();
builder.Services.AddCatalogIdentityProvider();
builder.Services.AddCatalogImageStorage(builder.Configuration);
builder.Services.AddCatalogCors(builder.Configuration);
builder.Services.AddCatalogRateLimiting(builder.Configuration);

var app = builder.Build();

app.UseForwardedHeaders();
app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapOpenApi();

app.MapGetHealth();
app.MapDefaultEndpoints();
app.MapGetMe();
app.MapRouteGroups();

app.Run();

public partial class Program;
