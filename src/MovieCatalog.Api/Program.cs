using System.Text.Json.Serialization;
using Asp.Versioning;
using HealthChecks.Redis;
using Microsoft.OpenApi.Models;
using MovieCatalog.Api.Middleware;
using MovieCatalog.Application;
using MovieCatalog.Infrastructure;
using MovieCatalog.Infrastructure.Persistence;
using Serilog;
using Serilog.Formatting.Compact;
using StackExchange.Redis;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.File(new CompactJsonFormatter(), "logs/movie-catalog-api-.json", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(new CompactJsonFormatter())
    .WriteTo.File(new CompactJsonFormatter(), "logs/movie-catalog-api-.json", rollingInterval: RollingInterval.Day));

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Movie Catalog API",
        Version = "v1",
        Description = "Catálogo de filmes da Cinemark: CRUD com cache-aside (Redis), soft delete e eventos de domínio publicados no SQS."
    });
    options.EnableAnnotations();

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddMovieCatalogApplication(builder.Configuration);
builder.Services.AddMovieCatalogInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddMongoDb(sp => sp.GetRequiredService<MongoDbContext>().Client, name: "mongodb", tags: new[] { "ready" })
    .AddRedis(sp => sp.GetRequiredService<IConnectionMultiplexer>(), name: "redis", tags: new[] { "ready" });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var mongoContext = scope.ServiceProvider.GetRequiredService<MongoDbContext>();
    await MongoIndexInitializer.EnsureIndexesAsync(mongoContext);
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Movie Catalog API v1");
});

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSerilogRequestLogging();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
