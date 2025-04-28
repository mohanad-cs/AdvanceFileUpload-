using AdvanceFileUpload.API;
using AdvanceFileUpload.API.Middleware;
using AdvanceFileUpload.Application.Hubs;
using AdvanceFileUpload.Application.Shared;
using Microsoft.OpenApi.Models;
using Serilog;
var builder = WebApplication.CreateBuilder(args);
builder.ConfigureUploadServer(builder.WebHost);
//===========================================================
const string logOutPutTempleate = "[{Timestamp:yyyy-MM-dd h:mm:ss tt} {Level:u12}] {Message:lj}{NewLine}{Exception}";
Log.Logger = new LoggerConfiguration()
     .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: logOutPutTempleate)
    .WriteTo.EventLog("AdvanceFileUploadAPI", "AdvanceFileUploadAPI", manageEventSource: true)
    .CreateLogger();

builder.Logging.ClearProviders().AddSerilog(Log.Logger);
// Add services to the container.
builder.Services.ConfigureApplicationServices(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddSignalR();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Advance File Upload API", Version = "v1" });
    options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());
    // Define the API Key security scheme
    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key Authentication",
        Type = SecuritySchemeType.ApiKey,
        Name = "X-APIKEY",
        In = ParameterLocation.Header,
        Scheme = "ApiKey"

    });
    // Require the API key for all endpoints
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                },
                Scheme = "ApiKey",
                Name = "X-APIKEY",
                In = ParameterLocation.Header
            },
            new List<string>()
        }
    });
});
//builder.Services.AddHttpsRedirection(options =>
//{
//    options.HttpsPort = 443;
//});

var app = builder.Build();
//app.UseHttpsRedirection();
// ==============================================
// Application Startup
// ==============================================

// Log configured endpoints
//app.Logger.LogInformation("Configured Kestrel Endpoints:");
//foreach (var endpoint in app.Urls)
//{
//    app.Logger.LogInformation("{Endpoint}: {Protocol}://{IP}:{Port}",
//        endpoint.Key,
//        endpoint.Value.Https ? "https" : "http",
//        endpoint.Value.Ip,
//        endpoint.Value.Port);
//}
//app.UseDeveloperExceptionPage();
//app.EnsureDbMigration();
app.UseRateLimiter();
app.UseSwagger(op =>
{
    op.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0;

});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("../swagger/v1/swagger.json", "Advance File Upload API");
});
app.UseMiddleware<APIKeyMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
//app.UseHttpsRedirection();
app.MapControllers();
app.MapHub<UploadProcessHub>(RouteTemplates.UploadProcessHub);
app.MapHealthChecks("/health");
app.Run();
