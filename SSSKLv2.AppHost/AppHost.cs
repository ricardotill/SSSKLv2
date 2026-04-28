var builder = DistributedApplication.CreateBuilder(args);

var sql = builder.AddSqlServer("sqlserver").WithDataVolume();
var db = sql.AddDatabase("db");

var storage = builder.AddAzureStorage("storage").RunAsEmulator(c => c.WithDataVolume());
var blobs = storage.AddBlobs("blobs");

var backend = builder.AddProject<Projects.SSSKLv2>("sssklv2")
    .WithEndpoint("http", endpoint =>
    {
        endpoint.Port = 5251;
    })
    .WithUrl("/scalar")
    .WithReference(db)
    .WithReference(blobs)
    .WaitFor(db)
    .WaitFor(blobs);

builder.AddNpmApp("frontend", "../Frontend")
    .WithReference(backend)
    .WaitFor(backend)
    .WithHttpsEndpoint(port: 4200, env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

await builder.Build().RunAsync();
