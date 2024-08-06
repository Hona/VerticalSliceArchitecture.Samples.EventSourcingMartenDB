using EventSourcedSandbox.Common.Marten;
using EventSourcedSandbox.Common.Seed;
using FastEndpoints.Swagger;

[assembly: VogenDefaults(customizations: Customizations.AddFactoryMethodForGuids)]

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFastEndpoints(options =>
{
    options.SourceGeneratorDiscoveredTypes.AddRange(EventSourcedSandbox.DiscoveredTypes.All);
});
builder.Services.SwaggerDocument();

builder.Services.AddAppMarten(builder.Configuration.GetConnectionString("DefaultConnection")!);

var app = builder.Build();

var seeder = new SeedService();
await seeder.SeedSampleDataAsync(app.Services);

app.UseFastEndpoints();
app.UseSwaggerGen();

app.Run();
