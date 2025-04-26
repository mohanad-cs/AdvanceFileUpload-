using AdvanceFileUpload.API.Middleware;
using AdvanceFileUpload.Application.Hubs;
using AdvanceFileUpload.Application.Shared;
using AdvanceFileUpload.API;
using Microsoft.OpenApi.Models;
using Serilog;
var builder = WebApplication.CreateBuilder(args);
const string  logOutPutTempleate= "[{Timestamp:yyyy-MM-dd h:mm:ss tt} {Level:u12}] {Message:lj}{NewLine}{Exception}";
Log.Logger = new LoggerConfiguration()
     .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: logOutPutTempleate)
    .WriteTo.EventLog("AdvanceFileUploadAPI", "AdvanceFileUploadAPI", manageEventSource: true)
    .CreateLogger();

builder.Host.ConfigureLogging(logging =>
{
    logging.ClearProviders();
    logging.AddSerilog(Log.Logger);
});
// Add services to the container.
builder.Services.ConfigureApplicationServices(builder.Configuration);
builder.Services.AddControllers();
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
builder.Services.AddSignalR();

var app = builder.Build();
app.UseDeveloperExceptionPage();
//app.EnsureDbMigration();
app.UseRateLimiter();
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger(op =>
//    {
//        op.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0;
       
//    });
//    app.UseSwaggerUI();
//}
app.UseSwagger(op =>
{
    op.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi2_0;

});
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("../swagger/v1/swagger.json","Advance File Upload API");
}); app.UseMiddleware<APIKeyMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHub<UploadProcessHub>(RouteTemplates.UploadProcessHub);
app.MapHealthChecks("/health");

app.Run();
