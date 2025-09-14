using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("CacheRedis");

var postgres = builder.AddPostgres("Postgres")
    .WithDataVolume()
    .WithPgAdmin();

if (builder.Environment.IsDevelopment())
    postgres.WithHostPort(5432);

var identityDb = postgres.AddDatabase("IdentityDb");
var coreDb = postgres.AddDatabase("CoreDb");

var apiCore = builder.AddProject<Projects.InitialAspireProject_ApiCore>("ApiCore")
    .WithHttpHealthCheck("/health")
    .WithReference(coreDb)
    .WaitFor(coreDb);

var apiIdentity = builder.AddProject<Projects.InitialAspireProject_ApiIdentity>("ApiIdentity")
    .WithHttpHealthCheck("/health")
    .WithReference(identityDb)
    .WaitFor(identityDb);

builder.AddProject<Projects.InitialAspireProject_Web>("Web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiIdentity)
    .WaitFor(apiIdentity)
    .WithReference(apiCore)
    .WaitFor(apiCore);

builder.Build().Run();