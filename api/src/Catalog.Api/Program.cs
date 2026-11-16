using Catalog.Api.Features.Health;
using Catalog.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapOpenApi();

app.MapGetHealth();
app.MapRouteGroups();

app.Run();

public partial class Program;
