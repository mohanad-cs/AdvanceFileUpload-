using AdvanceFileUpload.API;
using AdvanceFileUpload.API.Middleware;
using AdvanceFileUpload.Application.Hubs;
using AdvanceFileUpload.Application.Shared;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
var builder = WebApplication.CreateBuilder(args);


// ==============================================
// 1. Load and Validate Configuration
// ==============================================
builder.Services.Configure<KestrelConfiguration>(builder.Configuration.GetSection(KestrelConfiguration.SectionName));
KestrelConfiguration? serverConfiguration = (KestrelConfiguration?)builder.Configuration.GetRequiredSection(KestrelConfiguration.SectionName).Get<KestrelConfiguration>();
if (serverConfiguration is null)
{
    throw new InvalidOperationException("serverConfigurationuration have not been Configured");

}

ValidateConfiguration(serverConfiguration);

// ==============================================
// 2. Configure Kestrel Server
// ==============================================
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    // ----------------------------------
    // Global Server Limits
    // ----------------------------------
    var limits = serverConfiguration.Limits;
    serverOptions.Limits.KeepAliveTimeout = limits.KeepAliveTimeout;
    serverOptions.Limits.RequestHeadersTimeout = limits.RequestHeadersTimeout;
    serverOptions.Limits.MaxConcurrentConnections = limits.MaxConcurrentConnections;
    serverOptions.Limits.MaxConcurrentUpgradedConnections = limits.MaxConcurrentUpgradedConnections;
    serverOptions.Limits.MaxRequestBodySize = limits.MaxRequestBodySize;
    serverOptions.Limits.MinRequestBodyDataRate = limits.MinRequestBodyDataRate.HasValue
        ? new MinDataRate(bytesPerSecond: limits.MinRequestBodyDataRate.Value, gracePeriod: TimeSpan.FromSeconds(5))
        : null;
    serverOptions.Limits.MinResponseDataRate = limits.MinResponseDataRate.HasValue
        ? new MinDataRate(bytesPerSecond: limits.MinResponseDataRate.Value, gracePeriod: TimeSpan.FromSeconds(5))
        : null;
    serverOptions.Limits.MaxRequestLineSize = limits.MaxRequestLineSize;
    serverOptions.Limits.MaxRequestBufferSize = limits.MaxRequestBufferSize;
    serverOptions.Limits.MaxResponseBufferSize = limits.MaxResponseBufferSize;
    serverOptions.Limits.MaxRequestHeadersTotalSize = limits.MaxRequestHeadersTotalSize;
    serverOptions.Limits.MaxRequestHeaderCount= limits.MaxRequestHeaderCount;
    serverOptions.AllowSynchronousIO = limits.AllowSynchronousIO;

    // ----------------------------------
    // HTTP Protocol Configuration
    // ----------------------------------
    // HTTP/2 Settings
    serverOptions.Limits.Http2.MaxStreamsPerConnection = serverConfiguration.Http2.MaxStreamsPerConnection;
    serverOptions.Limits.Http2.HeaderTableSize = serverConfiguration.Http2.HeaderTableSize;
    serverOptions.Limits.Http2.MaxFrameSize = serverConfiguration.Http2.MaxFrameSize;

    // HTTP/3 Support
   // serverOptions.EnableAltSvc = serverConfiguration.Kestrel.Http3.Enable;

    // ----------------------------------
    // Global HTTPS Defaults
    // ----------------------------------
    serverOptions.ConfigureHttpsDefaults(httpsOptions =>
    {
        httpsOptions.SslProtocols = GetSslProtocols(serverConfiguration.Https.AllowedProtocols);
        httpsOptions.ClientCertificateMode = Enum.Parse<ClientCertificateMode>(serverConfiguration.Https.ClientCertificateMode);
        httpsOptions.CheckCertificateRevocation = serverConfiguration.Https.CheckCertificateRevocation;
    });

    // ----------------------------------
    // Endpoint Configuration
    // ----------------------------------
    foreach (var endpoint in serverConfiguration.Endpoints)
    {
        var ip = IPAddress.Parse(endpoint.Value.Ip);
        var port = endpoint.Value.Port;
        var protocols = GetHttpProtocols(endpoint.Value.Protocols);

        serverOptions.Listen(ip,port, listenOptions =>
        {
            listenOptions.Protocols = protocols;
         
            if (endpoint.Value.Https)
            {
                listenOptions.UseHttps(httpsOptions =>
                {
                    // Certificate Loading Logic
                    if (!string.IsNullOrWhiteSpace(endpoint.Value.Certificate.Path))
                    {
                        // Load from file
                        //var certPassword = builder.Configuration.GetValue<string>("CERT_PASSWORD");
                        httpsOptions.ServerCertificate = new X509Certificate2(
                            endpoint.Value.Certificate.Path,
                            endpoint.Value.Certificate.Password
                        );
                    }
                    else if (!string.IsNullOrEmpty(endpoint.Value.Certificate.Subject))
                    {
                        // Load from certificate store
                        using var store = new X509Store(
                            endpoint.Value.Certificate.Store,
                            Enum.Parse<StoreLocation>(endpoint.Value.Certificate.Location)
                        );
                        store.Open(OpenFlags.ReadOnly);
                        var certs = store.Certificates.Find(
                            X509FindType.FindBySubjectName,
                            endpoint.Value.Certificate.Subject,
                            validOnly: true
                        );
                        httpsOptions.ServerCertificate = certs.Count > 0
                            ? certs[0]
                            : throw new InvalidOperationException($"Certificate not found: {endpoint.Value.Certificate.Subject}");
                    }

                    // Endpoint-specific HTTPS settings
                    httpsOptions.SslProtocols = GetSslProtocols(endpoint.Value.SslProtocols);
                    httpsOptions.ClientCertificateMode = Enum.Parse<ClientCertificateMode>(endpoint.Value.ClientCertificateMode);
                });
            }
        });
    }
});

// ==============================================
// Helper Methods
// ==============================================

/// <summary>
/// Validate critical configuration values
/// </summary>
static void ValidateConfiguration(KestrelConfiguration config)
{
    if (config?.Endpoints == null || !config.Endpoints.Any())
    {
        throw new InvalidOperationException("Kestrel endpoints configuration is missing");
    }

    if (config.Limits == null)
    {
        throw new InvalidOperationException("Kestrel limits configuration is missing");
    }
    foreach (var endpoint in config.Endpoints)
    {
        if (endpoint.Value.Https)
        {
            if (string.IsNullOrEmpty(endpoint.Value.Certificate.Path) &&
                string.IsNullOrEmpty(endpoint.Value.Certificate.Subject))
            {
                throw new InvalidOperationException(
                    $"HTTPS endpoint '{endpoint.Key}' requires certificate configuration");
            }
        }
    }
    // validate the serverConfiguration.Kestrel.Limits;
    if (config.Limits.MaxConcurrentConnections <= 0)
    {
        throw new InvalidOperationException("MaxConcurrentConnections must be greater than 0");
    }
    if (config.Limits.MaxConcurrentUpgradedConnections <= 0)
    {
        throw new InvalidOperationException("MaxConcurrentUpgradedConnections must be greater than 0");
    }
    if (config.Limits.MaxRequestBodySize <= 0)
    {
        throw new InvalidOperationException("MaxRequestBodySize must be greater than 0");
    }
    if (config.Limits.MaxRequestLineSize <= 0)
    {
        throw new InvalidOperationException("MaxRequestLineSize must be greater than 0");
    }
    if (config.Limits.MaxRequestBufferSize <= 0)
    {
        throw new InvalidOperationException("MaxRequestBufferSize must be greater than 0");
    }
    if (config.Limits.MaxResponseBufferSize <= 0)
    {
        throw new InvalidOperationException("MaxResponseBufferSize must be greater than 0");
    }
    if (config.Limits.MaxRequestHeadersTotalSize <= 0)
    {
        throw new InvalidOperationException("MaxRequestHeadersTotalSize must be greater than 0");
    }
    if (config.Limits.MaxRequestHeaderCount <= 0)
    {
        throw new InvalidOperationException("MaxRequestHeaderCount must be greater than 0");
    }
    if (config.Limits.MinRequestBodyDataRate.HasValue && config.Limits.MinRequestBodyDataRate.Value <= 0)
    {
        throw new InvalidOperationException("MinRequestBodyDataRate must be greater than 0");
    }
    if (config.Limits.MinResponseDataRate.HasValue && config.Limits.MinResponseDataRate.Value <= 0)
    {
        throw new InvalidOperationException("MinResponseDataRate must be greater than 0");
    }
    
}

/// <summary>
/// Convert string list to SslProtocols enum
/// </summary>
static SslProtocols GetSslProtocols(List<string> protocols)
{
    var result = SslProtocols.None;
    foreach (var protocol in protocols)
    {
        result |= Enum.Parse<SslProtocols>(protocol);
    }
    return result;
}

/// <summary>
/// Convert string list to HttpProtocols enum
/// </summary>
static HttpProtocols GetHttpProtocols(List<string> protocols)
{
    var result = HttpProtocols.None;
    foreach (var protocol in protocols)
    {
        result |= Enum.Parse<HttpProtocols>(protocol);
    }
    return result;
}





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



var app = builder.Build();
// ==============================================
// Application Startup
// ==============================================

// Log configured endpoints
app.Logger.LogInformation("Configured Kestrel Endpoints:");
foreach (var endpoint in serverConfiguration.Endpoints)
{
    app.Logger.LogInformation("{Endpoint}: {Protocol}://{IP}:{Port}",
        endpoint.Key,
        endpoint.Value.Https ? "https" : "http",
        endpoint.Value.Ip,
        endpoint.Value.Port);
}
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
