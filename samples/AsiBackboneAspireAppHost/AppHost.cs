var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.CDCavell_AsiBackbone_Samples_PlainAspNetCoreHost>("asi-backbone-api")
    .WithEnvironment("ConnectionStrings__AsiBackbone", "Data Source=asi-backbone-aspire-sample.db")
    .WithEnvironment("ASIBACKBONE_SAMPLE_HOST", "AspireAppHost")
    .WithEnvironment("ASIBACKBONE_SAMPLE_DOCS", "https://cdcavell.github.io/AsiBackbone/articles/aspire-apphost-sample.html");

api.WithUrlForEndpoint("http", url =>
{
    url.DisplayText = "Sample decision endpoint";
    url.Url = "/sample/decision";
});

builder.Build().Run();
