# **Configuration Settings**

The Advance Chunk File Upload API is configured primarily through the appsettings.json file. This document explains the available settings and how they affect the application's behavior.

## **UploadSetting** (Required)

This section controls the core file upload parameters.

| Key | Type | Description | Default |
| :---- | :---- | :---- | :---- |
| AllowedExtensions | string\[\] | An array of allowed file extensions (e.g., ".jpg", ".pdf"). | N/A |
| MaxFileSize | long | The maximum allowed size for a single file, in bytes. | 1000000000 (1 GB) |
| MaxChunkSize | long | The size of each chunk for uploads, in bytes. | 2097152 (2 MB) |
| SavingDirectory | string | The absolute path to the directory where completed files are stored. | N/A |
| TempDirectory | string | The absolute path to the directory where temporary chunks are stored during upload. | N/A |
| EnableCompression | bool | If true, allows clients to request file compression before uploading. | true |
| EnableIntegrationEventPublishing | bool | If true, the server will publish events (e.g., SessionCompleted) to a RabbitMQ message bus. | true |

**Example:**
```json
"UploadSetting": {  
  "AllowedExtensions": [ ".jpg", ".png", ".pdf", ".mp4" ],  
  "MaxFileSize": 1073741824,  
  "MaxChunkSize": 2097152,  
  "SavingDirectory": "D:\\\\Temp\\\\Savedir",  
  "TempDirectory": "D:\\\\Temp\\\\T",  
  "EnableCompression": true,  
  "EnableIntegrationEventPublishing": true  
}
```
## **APIKeyOptions** (Required)

This section manages client authentication and rate limiting.

| Key | Type | Description | Default |
| :---- | :---- | :---- | :---- |
| EnableAPIKeyAuthentication | bool | If true, all API requests must include a valid X-APIKEY header. | true |
| EnableRateLimiting | bool | If true, requests are throttled based on the RateLimit settings for each API key. | true |
| DefaultMaxRequestsPerMinute | int | The rate limit applied to clients with a valid key that does not have a specific RateLimit defined. | 1000 |
| ApiKeys | APIKey\[\] | An array of client API key configurations. | \[\] |

### **APIKey Object** 

| Key | Type | Description |
| :---- | :---- | :---- |
| ClientId | string | A friendly name to identify the client. |
| Key | string | The secret API key string. |
| RateLimit | object | An object defining the rate limit for this specific key. Contains RequestsPerMinute (int). |

**Example:**
```json
"APIKeyOptions": {  
  "EnableAPIKeyAuthentication": true,  
  "EnableRateLimiting": true,  
  "DefaultMaxRequestsPerMinute": 1000,  
  "ApiKeys": [  
    {  
      "ClientId": "ClientA",  
      "Key": "secret-key-for-client-a",  
      "RateLimit": { "RequestsPerMinute": 2000 }  
    }  
  ]  
}
```
## **ConnectionStrings** (Required)

This section contains the database connection strings.

| Key | Description |
| :---- | :---- |
| SessionStorage | The connection string for the SQL Server database used to store upload session data. |

## **RabbitMQOptions** (Optional)

This section is used to configure the connection to the RabbitMQ message broker.

| Key | Type | Description | Default |
| :---- | :---- | :---- | :---- |
| HostName | string | The hostname of the RabbitMQ server. | localhost |
| UserName | string | The username for connecting to RabbitMQ. | guest |
| Password | string | The password for connecting to RabbitMQ. | guest |
| Port | int | The port number for the RabbitMQ server. | 5672 |

## **Kestrel Server Configuration** (Required) 
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
   See [Kestrel Configuration](http://185.227.109.88:80/api/AdvanceFileUpload.API.KestrelConfiguration.html) For More information.
