# **Server Installation and Setup**

This guide provides detailed, step-by-step instructions for downloading the source code, configuring the necessary settings, and running the Advance Chunk File Upload API server. It also includes a comprehensive example of how to integrate and use the client SDK to perform your first file upload.

## **1\. Download the Source Code**

First, you need to obtain the complete source code from the official GitHub repository. This repository contains all the necessary projects, including the API server, the client SDK library, and sample applications.

Clone the repository using Git:

git clone https://github.com/mohanad-cs/AdvanceFileUpload.git  
cd AdvanceFileUpload

## **2\. Configure the Server**

The server's behavior is primarily controlled by the appsettings.json file, located in the AdvanceFileUpload.API project. This file allows you to tailor the API to your specific environment without modifying the source code.

1. **Open appsettings.json**: Navigate to the AdvanceFileUpload.API/ directory and open the appsettings.json file in a text editor.  
2. **Configure Connection String**: 
>[!IMPORTANT]
>Update the SessionStorage connection string to point to your SQL Server instance. This database is critical for the system's operation, as it stores and tracks the state of every upload session, including which chunks have been received, enabling the core pause and resume functionality. 

```json 
   "ConnectionStrings": {  
     "SessionStorage": "Server=YOUR_SERVER_INSTANCE;Database=SessionStorageDb;TrustServerCertificate=true;TrustedConnection=True;"  
   }
```
3. **Configure File Paths**: 
>[!IMPORTANT]
>You must define two important directory paths. Ensure that these directory locations exist and that the account running the API service has full read/write permissions to them. 
* SavingDirectory: The final destination for fully assembled files after a successful upload.  
* TempDirectory: A working directory for storing temporary chunks during the upload process. These chunks are automatically deleted after the upload is completed or canceled.

```json
"UploadSetting": {  
  "SavingDirectory": "D:\\\\Path\\\\To\\\\SavedFiles",  
  "TempDirectory": "D:\\\\Path\\\\To\\\\TempChunks"  
}
```
4. **Configure API Keys**: Add one or more API keys that client applications will use to authenticate with the API. This is a security measure to ensure that only authorized clients can upload files. Each key can also be assigned a unique rate limit.  
```json
   "APIKeyOptions": {  
     "ApiKeys": [  
       {  
         "ClientId": "MyWebApp",  
         "Key": "your-super-secret-api-key",  
         "RateLimit": {  
           "RequestsPerMinute": 2000  
         }  
       }  
     ]  
   }
```
## **3\. Run the API Server**

Once configured, you can launch the server from the root of the AdvanceFileUpload.API project. On its first run, Entity Framework Core migrations will automatically execute, creating the necessary database schema (FileUploadSessions and ChunkFiles tables) if it doesn't already exist.

cd AdvanceFileUpload.API  
dotnet run

You should see console output indicating that the Kestrel server has started and is now listening for requests, by default at http://localhost:5124.

## **4\. Using the Client SDK**

The AdvanceFileUpload.Client library provides the easiest and most reliable way to interact with the API.

1. **Add the Client Library**: In your client application project (e.g., a Console App, WPF, or ASP.NET Core app), add a project reference to the AdvanceFileUpload.Client project.  
2. **Initiate an Upload**: Use the FileUploadBuilder class, which provides a fluent interface to easily configure the service and start an upload.  
```c#
   using AdvanceFileUpload.Client;  
   using AdvanceFileUpload.Application.Request; // Required for CompressionOption

   // 1\. Configure the service using the fluent builder.  
   // This pattern makes configuration readable and less prone to errors.  
   IFileUploadService fileUploadService = FileUploadBuilder  
       .New("API Address", "your-super-secret-api-key") // Set the API URL and your secret key  
       .WithMaxConcurrentUploads(8) // Boosts performance by uploading 8 chunks at once  
       .WithMaxRetriesCount(5)      // Enhances reliability by retrying failed chunks 5 times  
       .WithCompressionOption(new CompressionOption // Reduces bandwidth usage  
       {  
           Algorithm = CompressionAlgorithmOption.GZip,  
           Level = CompressionLevelOption.Fastest  
       })  
       .Build();

   // 2\. Subscribe to events for real-time feedback and robust error handling.  
   fileUploadService.UploadProgressChanged += (s, e) => {  
       Console.WriteLine($"Progress: {e.ProgressPercentage:F2}% | Status: {e.UploadStatus}");  
   };

   fileUploadService.SessionCompleted += (s, e) => {  
       Console.WriteLine($"Upload complete\! Session ID: {e.SessionId}");  
   };

   fileUploadService.UploadError += (s, e) => {  
       Console.WriteLine($"An error occurred: {e}");  
   };

   try  
   {  
       // 3\. Start the upload process.  
       string filePath = @"C:\\path\\to\\your\\large-file.zip";  
       await fileUploadService.UploadFileAsync(filePath);  
   }  
   catch (UploadException ex)  
   {  
       // Catch specific exceptions from the upload service.  
       Console.WriteLine($"Upload failed: {ex.Message}");  
   }  
   finally  
   {  
       // 4\. Always dispose the service to clean up resources,  
       // including any temporary files created during the process.  
       fileUploadService.Dispose();  
   }  
  ```
