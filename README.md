# AdvanceFileUpload

AdvanceFileUpload is solution designed to handle efficient file Uploading. The project leverages modern asynchronous and parallel programming techniques to optimize performance, minimize memory usage, and ensure scalability.

## Features

- **chunk Uploading**: Split large files into smaller chunks for easier processing and storage.
- **Parallel Processing**: Supports configurable parallelism for faster chunk processing.
- **Compression Support**: Configurable file compression to reduce upload size.Supported algorithms: GZip, Brotli, Deflate.
- **Real Time Notfication**: using SgnalR to notify of Uploading Process.
- **Retry Mechanism**: Automatically retries failed uploads with exponential backoff.
- **Pause/Resume**: Allows pausing and resuming uploads for better user control.
- **Health Monitoring**: Monitors API health and handles degraded or unhealthy states gracefully.
- **Integration Supports**: Supporting Integration with other systems via RabbitMQ.


## GetStarted
### Prerequisites
- [ASP.NET Core 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)
- A configured SQL Server database for session storage.
- RabbitMQ for integration event publishing (optional).
### Server Installation
1. **Download the Latest API Server Release**  
   Download the latest release of the API server from [API Release](#).

2. **Configure the Server**  
   Follow these steps to configure the server:

   - **Database Configuration** (Required):  
     Update the connection string in the `appsettings.json` file under the `SessionStorage` section.  
     Example:
      ```json
      {
      "ConnectionStrings": {
      "SessionStorage": "Server=localhost;Database=FileUploadDB;User Id=sa;Password=your_password;"
      }
      }
     ```
   - **Upload Settings** (Required):  
     Configure the upload settings in the `appsettings.json` file under the `UploadSetting` section.  
     Example:
    ```json
      {
   "UploadSetting": {
     "AllowedExtensions": [".jpg", ".png", ".pdf", ".mp4"],
     "MaxChunkSize": 10485760,
     "MaxFileSize": 1073741824,
     "SavingDirectory": "C:\\Uploads",
     "TempDirectory": "C:\\Temp",
     "EnableCompression": true,
     "EnableIntegrationEventPublishing": true
     }
     }
    ```
     - **API Keys and Rate Limiting** (Required):  
   Configure the API keys and API rate limiting in the `appsettings.json` file under the `ApiKeyOptions` section.  
   Example:
    ```json
      {
    "ApiKeyOptions": {
     "EnableRateLimiting": true,
     "EnableAPIKeyAuthentication": true,
     "DefaultMaxRequestsPerMinute": 1000,
     "APIKeys": [
       {
         "Key": "your_api_key",
         "ClientId": "client_1",
         "RateLimit": {
           "RequestsPerMinute": 500
         }
       }
     ]
      }
     }
    ```
     - **RabbitMQ Configuration** (Optional):  
   If you want to enable integration event publishing, configure RabbitMQ settings in the `appsettings.json` file under the `RabbitMQOptions` section.  
   Example:
   ```json
   {
        "RabbitMQOptions": {
        "HostName": "localhost",
        "UserName": "guest",
        "Password": "guest",
        "Port": 5672,
        "VirtualHost": "/",
        "UseSSL": false
    },
   }
   ```

  - **Kestrel Server Configuration** (Required):  
  Configure Kestrel Server settings in the `appsettings.json` file under the `KestrelConfiguration` section.  
   Example: 
   ```json
   {
     "KestrelConfiguration": {
        "Endpoints": {
            "PublicHttp": {
                "Ip": "Your server IP",
                "Port": 5003,
                "Https": false, //  true or false. If true, the endpoint will use HTTPS.
                "Protocols": [ "Http1", "Http2" ], //  Options include "Http1", "Http2" "Http3".
                "SslProtocols": [ "Default"],
                "Certificate": {
                    "Subject": "CN=localhosttest",
                    "Store": "Root",
                    "Location": "LocalMachine" // "LocalMachine" or "CurrentUser".
                }
            }
        }
    },
    "Limits": {
        "KeepAliveTimeout": "00:02:00", // 5m
        "RequestHeadersTimeout": "00:02:00", // 3m
        "MaxConcurrentConnections": 10000,
        "MaxConcurrentUpgradedConnections": 500, //100,
        "MaxRequestBodySize": 30000000, // 28.6MB
        "MinRequestBodyDataRate": null, // 240byte/MinRequestBodyDataRatePeriod
        "MinRequestBodyDataRatePeriod": "00:00:15", // 15s
        "MinResponseDataRate": null, // 240byte/ MinResponseDataRatePeriod
        "MinResponseDataRatePeriod": "00:00:15",
        "MaxRequestLineSize": 81234543, //8192, // 8KB
        "MaxRequestBufferSize": null, //1048576, // 1MB
        "MaxResponseBufferSize": null, // 65536, // 64KB
        "MaxRequestHeadersTotalSize": 32768, // 32KB
        "MaxRequestHeadersCount": 100, // 100 headers
        "AllowSynchronousIO": false
    },
    "Https": {
        "CheckCertificateRevocation": true,
        "ClientCertificateMode": "AllowCertificate", //  Options include "NoCertificate", "AllowCertificate", and "RequireCertificate".
        "AllowedProtocols": [ "Default", "Tls12", "Tls13", "Ssl2" ] // "None", "Ssl2", "Ssl3", "Tls","Default","Tls11","Tls12", "Tls13"
    },
    "Http2": {
        "MaxStreamsPerConnection": 100,
        "HeaderTableSize": 4096, // 4KIB
        "MaxFrameSize": 16384 // 16KIB   Min:16384 Max: 16777215
    },
    "Http3": {
        "Enable": false
    }
   }
   ```
   See [KestrelConfiguration](http://103.89.14.244:8080/api/AdvanceFileUpload.API.KestrelConfiguration.html) For More information.

### Make The First Upload
 Install The AdvanceFileUpload.Client NuGet package.
1. **Setup the Client**  
   Create a new instance of the `FileUploadService` in your client application:
   ```C#
   var uploadOptions = new UploadOptions 
   { 
    APIKey = "your_api_key",
    TempDirectory = "D:\Temp", 
    MaxConcurrentUploads = 4,
    MaxRetriesCount = 3, 
    CompressionOption = new CompressionOption
     {
         Algorithm = CompressionAlgorithmOption.GZip,
          Level = CompressionLevelOption.Optimal 
     }
    };
   var fileUploadService = new FileUploadService(new Uri("http://yourServerIp:5021"), uploadOptions);
   ```
 2. **Subscribe to Events**  
   Subscribe to lifecycle events for real-time updates:
   ```C#
   fileUploadService.SessionCreated += (sender, e) => { Console.WriteLine($"Session Created: {e.SessionId}, Total Chunks: {e.TotalChunksToUpload}"); };
   fileUploadService.UploadProgressChanged += (sender, e) => { Console.WriteLine($"Progress: {e.ProgressPercentage}%"); };
   fileUploadService.SessionCompleted += (sender, e) => { Console.WriteLine($"Upload Completed: {e.FileName}"); };
   fileUploadService.UploadErroe+= (sender, e) => { Console.WriteLine($"Upload Error: {e}"); };
   ```
   3. **Upload a File**  
   Call the `UploadFileAsync` method to start uploading:
   ```C#
    await fileUploadService.UploadFileAsync("D:\Temp\largefile.mp4");
   ```
   4. **Pause/Resume/Cancel**  
   Use the following methods to control the upload:
   ```C#
   // Pause
   await fileUploadService.PauseUploadAsync();
   // Resume
    await fileUploadService.ResumeUploadAsync();
    // Cancel
    await fileUploadService.CancelUploadAsync(); 
   ```

## Sample
You Can Try our Winform Sample

## Integration with The File Upload API

---
### Troubleshooting

- **Health Check Fails**:  
  Ensure the database and RabbitMQ are running and properly configured.

- **Upload Fails**:  
  Check the server logs for detailed error messages.

- **Rate Limiting**:  
  If you encounter `429 Too Many Requests`, adjust the rate-limiting settings in the `appsettings.json` file.

---
### Documentation
For more details, refer to the [Documentation](103.89.14.244:8080).
