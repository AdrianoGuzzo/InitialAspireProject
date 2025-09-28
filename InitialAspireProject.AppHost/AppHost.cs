using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cacheredis");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

if (builder.Environment.IsDevelopment())
    postgres.WithHostPort(5432);

var identityDb = postgres.AddDatabase("identitydb");
var coreDb = postgres.AddDatabase("coredb");

var apiCore = builder.AddProject<Projects.InitialAspireProject_ApiCore>("apicore")
    .WithHttpHealthCheck("/health")
    .WithReference(coreDb)
    .WaitFor(coreDb);

var apiIdentity = builder.AddProject<Projects.InitialAspireProject_ApiIdentity>("apiidentity")
    .WithHttpHealthCheck("/health")
    .WithReference(identityDb)
    .WaitFor(identityDb);

builder.AddProject<Projects.InitialAspireProject_Web>("web")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiIdentity)
    .WaitFor(apiIdentity)
    .WithReference(apiCore)
    .WaitFor(apiCore);

builder.Build().Run();