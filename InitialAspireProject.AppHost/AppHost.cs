using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var compose = builder.AddDockerComposeEnvironment("compose");

var cache = builder.AddRedis("cacheredis");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin()
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "postgres";
    });

if (builder.Environment.IsDevelopment())
    postgres.WithHostPort(5432);

var identityDb = postgres.AddDatabase("identitydb");
var coreDb = postgres.AddDatabase("coredb");

var apiCore = builder.AddProject<Projects.InitialAspireProject_ApiCore>("apicore")
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "apicore";
    })
    .WithHttpHealthCheck("/health")
    .WithReference(coreDb)
    .WaitFor(coreDb);

var apiIdentity = builder.AddProject<Projects.InitialAspireProject_ApiIdentity>("apiidentity")
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "apiidentity";
    })
    .WithHttpHealthCheck("/health")
    .WithReference(identityDb)
    .WaitFor(identityDb);

builder.AddProject<Projects.InitialAspireProject_Web>("web")
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "web";
    })
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiIdentity)
    .WaitFor(apiIdentity)
    .WithReference(apiCore)
    .WaitFor(apiCore);

builder.Build().Run();