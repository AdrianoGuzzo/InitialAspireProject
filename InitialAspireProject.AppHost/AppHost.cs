using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var compose = builder.AddDockerComposeEnvironment("compose");

var mailpit = builder.AddMailPit("mailpit");

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

// Declare web first so its HTTPS endpoint can be passed to apiIdentity for password reset links
var web = builder.AddProject<Projects.InitialAspireProject_Web>("web")
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "web";
    })
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(cache)
    .WaitFor(cache);

var apiIdentity = builder.AddProject<Projects.InitialAspireProject_ApiIdentity>("apiidentity")
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "apiidentity";
    })
    .WithHttpHealthCheck("/health")
    .WithReference(identityDb)
    .WaitFor(identityDb)
    .WithReference(mailpit)
    .WaitFor(mailpit)
    .WithEnvironment("App__BaseUrl", web.GetEndpoint("https"));

var bff = builder.AddProject<Projects.InitialAspireProject_Bff>("bff")
    .PublishAsDockerComposeService((resource, service) =>
    {
        service.Name = "bff";
    })
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiIdentity)
    .WaitFor(apiIdentity)
    .WithReference(apiCore)
    .WaitFor(apiCore);

web.WithReference(apiIdentity)
    .WaitFor(apiIdentity)
    .WithReference(apiCore)
    .WaitFor(apiCore);

builder.Build().Run();