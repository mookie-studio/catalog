using System.Text.Json.Serialization;
using Catalog.Api.Data;
using Catalog.Api.Features.Health;
using Catalog.Api.Features.Me;
using Catalog.Api.Infrastructure;
using Catalog.Api.Infrastructure.Auth;
using Catalog.Api.Infrastructure.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddCatalogData();
builder.Services.AddCatalogAuth();
builder.Services.AddCatalogIdentityProvider();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();

app.MapGetHealth();
app.MapGetMe();
app.MapRouteGroups();

app.Run();

public partial class Program;
