// Local development only (ADR 0010). Supabase Authentication runs separately with `supabase start`;
// its URL and keys come from parameters (see docs/local-development.md for the user-secrets commands).

var builder = DistributedApplication.CreateBuilder(args);

const string bucket = "catalog-images";
var repositoryRoot = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "..", "..", ".."));

// Supabase (local CLI defaults where a value is not secret).
var supabaseUrl = builder.AddParameter("supabase-url", "http://127.0.0.1:54321");
var supabaseJwtSecret = builder.AddParameter("supabase-jwt-secret", secret: true);
var supabaseServiceRoleKey = builder.AddParameter("supabase-service-role-key", secret: true);
var supabaseAnonKey = builder.AddParameter("supabase-anon-key", secret: true);
var platformAdminUserId = builder.AddParameter("platform-admin-user-id", "00000000-0000-0000-0000-000000000000");

var postgres = builder.AddPostgres("postgres").WithDataVolume("catalog-postgres-data");
var catalogDb = postgres.AddDatabase("catalog");

// S3-compatible image storage standing in for Cloudflare R2 (same image as docker-compose.yml).
var storage = builder.AddContainer("storage", "adobe/s3mock", "3.12.0")
    .WithEnvironment("COM_ADOBE_TESTING_S3MOCK_STORE_INITIAL_BUCKETS", bucket)
    .WithEnvironment("COM_ADOBE_TESTING_S3MOCK_STORE_RETAIN_FILES_ON_EXIT", "true")
    .WithEnvironment("COM_ADOBE_TESTING_S3MOCK_STORE_ROOT", "/data")
    .WithVolume("catalog-storage-data", "/data")
    .WithHttpEndpoint(targetPort: 9090, name: "http");
var storageEndpoint = storage.GetEndpoint("http");

var api = builder.AddProject<Projects.Catalog_Api>("api")
    .WithReference(catalogDb, connectionName: "Catalog")
    .WaitFor(catalogDb)
    .WaitFor(storage)
    .WithEnvironment("Auth__Issuer", ReferenceExpression.Create($"{supabaseUrl}/auth/v1"))
    .WithEnvironment("Auth__JwtSecret", supabaseJwtSecret)
    .WithEnvironment("Auth__JwksUrl", ReferenceExpression.Create($"{supabaseUrl}/auth/v1/.well-known/jwks.json"))
    .WithEnvironment("Auth__PlatformAdminUserIds__0", platformAdminUserId)
    .WithEnvironment("Supabase__Url", supabaseUrl)
    .WithEnvironment("Supabase__ServiceRoleKey", supabaseServiceRoleKey)
    .WithEnvironment("Storage__ServiceUrl", storageEndpoint)
    .WithEnvironment("Storage__Bucket", bucket)
    .WithEnvironment("Storage__AccessKey", "local")
    .WithEnvironment("Storage__SecretKey", "local")
    .WithEnvironment("Storage__PublicBaseUrl", ReferenceExpression.Create($"{storageEndpoint}/{bucket}"));

// Migrations run only when started from the dashboard; the API never applies them at start-up.
// Requires the dotnet-ef global tool.
builder.AddExecutable("migrate", "dotnet", Path.Combine(repositoryRoot, "api"),
        "ef", "database", "update", "--project", "src/Catalog.Api")
    .WithReference(catalogDb, connectionName: "Catalog")
    .WaitFor(catalogDb)
    .WithExplicitStart();

// The SPA on http://localhost:5173 (the Supabase redirect URLs point there); Vite proxies /api to the API.
var web = builder.AddViteApp("web", Path.Combine(repositoryRoot, "web"))
    .WithNpm(install: true)
    .WithEndpoint("http", endpoint => endpoint.Port = 5173)
    .WithReference(api)
    .WaitFor(api)
    .WithEnvironment("API_PROXY_TARGET", api.GetEndpoint("http"))
    .WithEnvironment("VITE_SUPABASE_URL", supabaseUrl)
    .WithEnvironment("VITE_SUPABASE_ANON_KEY", supabaseAnonKey);

api.WithEnvironment("Supabase__InviteRedirectUrl", ReferenceExpression.Create($"{web.GetEndpoint("http")}/accept-invite"));

builder.Build().Run();
