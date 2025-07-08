# **How to Use the Client SDK**

The `AdvanceFileUpload.Client` SDK is a .NET library designed to dramatically simplify the process of interacting with the Advance Chunk File Upload API. It abstracts away the complexities of HTTP requests, session management, file chunking, parallel uploads, and error handling, allowing you to integrate robust file upload capabilities into your application with just a few lines of code.

This guide will walk you through installing the SDK, configuring the service, performing uploads, and handling events.

## **1\. Installation**

To use the SDK, you first need to add it to your .NET project.

1. **Add a Project Reference**: If you have the source code, the easiest way is to add a direct project reference from your application (e.g., a Console App, WPF, or ASP.NET project) to the AdvanceFileUpload.Client project within the solution.  
2. **Add from NuGet (Future)**: Once published, you would install it via the NuGet Package Manager:  
   Install-Package AdvanceFileUpload.Client

## **2\. Configuration with FileUploadBuilder**

The FileUploadBuilder class provides a fluent and intuitive interface for configuring the FileUploadService. This pattern makes the setup process clean, readable, and less error-prone.

The builder is initiated with the API's base URL and your authentication key.

using AdvanceFileUpload.Client;
```c#
// Start building the service with the required URL and API key  
IFileUploadService fileUploadService = FileUploadBuilder  
    .New("http://localhost:5124", "your-super-secret-api-key")  
    // ... chain additional configuration methods here ...  
    .Build();
```
### **Configuration Options**

You can chain the following methods to customize the service's behavior:

* **WithMaxConcurrentUploads(int count)**: Sets the number of chunks to upload in parallel. Increasing this can significantly improve upload speed, especially on high-latency networks. The default is 4.  
```c#
  .WithMaxConcurrentUploads(8)
```
* **WithMaxRetriesCount(int count)**: Configures the number of times the SDK will automatically retry a failed chunk upload. This is essential for resilience over unstable connections. The default is 3.  
```c#
  .WithMaxRetriesCount(5)
```
* **WithCompressionOption(CompressionOption option)**: Enables file compression to reduce bandwidth usage and transfer time. The API will not compress already-compressed file types (like .zip or .jpg).  
```c#
  .WithCompressionOption(new CompressionOption  
    {  
      Algorithm = CompressionAlgorithmOption.GZip,  
      Level = CompressionLevelOption.Fastest  
    });
```
* **WithTempDirectory(string path)**: Specifies a custom directory for storing temporary files (like compressed versions or chunks) during the upload process. The default is the system's temporary path.  
```c#
  .WithTempDirectory("C:\\\\MyApp\\\\TempUploads")
```
* **WithRequestTimeOut(TimeSpan timeout)**: Sets the timeout for individual HTTP requests for uploading chunks. The default is 30 seconds. 
```c# 
  .WithRequestTimeOut(TimeSpan.FromSeconds(60))
```
## **3\. Performing an Upload**

Once the FileUploadService is built, you can start an upload by calling UploadFileAsync with the path to the file. It's crucial to wrap the call in a try-catch-finally block to handle potential UploadException errors and to ensure the service is disposed of correctly, which cleans up any temporary files.
```c#
try  
{  
    string filePath = @"C:\\path\\to\\your\\large-video.mp4";  
    await fileUploadService.UploadFileAsync(filePath);  
    Console.WriteLine("Upload process initiated successfully.");  
}  
catch (UploadException ex)  
{  
    Console.WriteLine($"Upload failed: {ex.Message}");  
}  

```
## **4\. Handling Events**

The SDK provides a rich set of events to monitor every stage of the upload lifecycle. Subscribing to these events is the best way to provide real-time feedback to users or to log the process.

**Key Events:**

* UploadProgressChanged: Fires frequently with real-time progress updates, including percentage, status, and remaining chunks. Ideal for updating a progress bar.  
* SessionCreated: Fires after the server has successfully created an upload session.  
* ChunkUploaded: Fires every time a single chunk is successfully uploaded.  
* SessionCompleted: Fires once the file is fully uploaded, merged, and saved on the server.  
* UploadError: Fires when a non-recoverable error occurs.  
* SessionPaused / SessionResumed: Fires when the upload is paused or resumed.  
* SessionCanceled: Fires when the upload is successfully canceled.

**Example of Subscribing to Events:**
```c#
// Subscribe before calling UploadFileAsync  
fileUploadService.UploadProgressChanged += (sender, args) =>  
{  
    Console.WriteLine($"Progress: {args.ProgressPercentage:F2}% | Status: {args.UploadStatus}");  
};

fileUploadService.SessionCompleted += (sender, args) =>  
{  
    Console.WriteLine($"File '{args.FileName}' uploaded successfully\! Session ID: {args.SessionId}");    
    // Disposing the service is critical to clean up temporary files.  
    fileUploadService.Dispose();  
};

fileUploadService.UploadError += (sender, errorMessage) =>  
{  
    Console.WriteLine($"An error occurred during upload: {errorMessage}");  
};
```
## **5\. Controlling the Upload (Pause, Resume, Cancel)**

The SDK provides methods to control an in-progress upload. You can use the service's state properties (e.g., CanPauseSession) to enable or disable UI controls accordingly.

* **PauseUploadAsync()**: Halts the upload. The connection remains open, and the session is preserved on the server.  
* **ResumeUploadAsync()**: Resumes a paused upload. The SDK will automatically determine which chunks are missing and upload only those.  
* **CancelUploadAsync()**: Aborts the upload and instructs the server to delete any temporary data.

**Example Implementation:**
```c#
// In a UI event handler for a "Pause" button  
if (fileUploadService.CanPauseSession)  
{  
    await fileUploadService.PauseUploadAsync();  
}

// In a UI event handler for a "Resume" button  
if (fileUploadService.CanResumeSession)  
{  
    await fileUploadService.ResumeUploadAsync();  
}

// In a UI event handler for a "Cancel" button  
if (fileUploadService.CanCancelSession)  
{  
    await fileUploadService.CancelUploadAsync();  
}  
```

> [!WARNING]
> Service instances are single-use - if you want to upload multiple files in parallel, you need to create a new instance of the `FileUploadService` for each file.