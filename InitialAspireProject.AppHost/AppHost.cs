using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

// Adicionar PostgreSQL
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

if (builder.Environment.IsDevelopment())
    postgres.WithHostPort(5432);

var identityDb2 = postgres.AddDatabase("IdentityDb2");
var identityDb = postgres.AddDatabase("identitydb");
var apiDb = postgres.AddDatabase("apidb");

var apiIdentity= builder.AddProject<Projects.InitialAspireProject_ApiIdentity>("ApiIdentity")
    .WithHttpHealthCheck("/health")
    .WithReference(identityDb2)
    .WaitFor(identityDb2); ;

// Configurar o serviço de API com referência ao banco
var apiService = builder.AddProject<Projects.InitialAspireProject_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReference(identityDb)
    .WaitFor(postgres);

// Configurar o frontend web
builder.AddProject<Projects.InitialAspireProject_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithReference(apiIdentity)
    .WaitFor(apiIdentity);



builder.Build().Run();