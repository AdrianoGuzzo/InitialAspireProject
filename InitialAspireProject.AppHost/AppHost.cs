using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

// Adicionar PostgreSQL
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

if (builder.Environment.IsDevelopment())
    postgres.WithHostPort(5432);

var identityDb = postgres.AddDatabase("IdentityDb");

var apiIdentity= builder.AddProject<Projects.InitialAspireProject_ApiIdentity>("ApiIdentity")
    .WithHttpHealthCheck("/health")
    .WithReference(identityDb)
    .WaitFor(identityDb); ;

// Configurar o frontend web
builder.AddProject<Projects.InitialAspireProject_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiIdentity)
    .WaitFor(apiIdentity);

builder.Build().Run();