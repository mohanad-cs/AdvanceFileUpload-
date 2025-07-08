## Frequently Asked Questions (FAQ)

**Q**: What happens if the network connection drops during an upload?<br>
**A**: The system is designed to be resilient to network interruptions. The client SDK uses a retry policy (powered by Polly) to automatically re-attempt sending a failed chunk a configurable number of times.

If the connection is lost for an extended period, the upload will be paused. When the connection is restored, you can call the ResumeUploadAsync() method. The SDK will then query the server to find out which chunks are missing and will only upload those, continuing from where it left off. You do not need to restart the entire file upload.

**Q**: How does the resumable upload feature work?<br>
**A**: Resumability is managed through stateful sessions on the server. When an upload starts, the server creates a FileUploadSession in its database, identified by a unique SessionId. As each chunk is successfully uploaded, the server records it against that session.

When you call ResumeUploadAsync(), the client SDK asks the server for the status of that SessionId. The server checks its records and sends back a list of the chunk indexes that it has not yet received. The SDK then proceeds to upload only those missing chunks.

**Q:** What happens if a chunk fails to upload?<br>
**A:** You can retry uploading the same chunk. The session keeps track of uploaded chunks.

**Q**: Can I upload multiple files at the same time?<br>
**A**: Yes, but you must create a separate instance of the IFileUploadService for each concurrent upload. A single service instance is designed to manage the lifecycle of a single file upload.

To upload multiple files simultaneously, you would create and manage multiple FileUploadService objects, for example:
```C#
var uploadTask1 = StartUploadForFile("path/to/file1.zip");
var uploadTask2 = StartUploadForFile("path/to/file2.zip");

await Task.WhenAll(uploadTask1, uploadTask2);

async Task StartUploadForFile(string filePath)
{
    // Each upload gets its own service instance
    IFileUploadService uploader = FileUploadBuilder.New(...).Build();
    try
    {
        await uploader.UploadFileAsync(filePath);
    }
}
```

**Q**: What file types should not be compressed?<br>
**A**: You should avoid compressing files that are already in a compressed format. Attempting to re-compress these files provides little to no size reduction and wastes CPU resources on both the client and the server. The client SDK is pre-configured to automatically skip compression for many of these file types.

Examples of file types that should not be compressed include:

Archives: .zip, .rar, .7z, .gz
Images: .jpg, .jpeg, .png, .gif
Videos: .mp4, .mov, .mkv
Audio: .mp3, .aac
Documents: .pdf, .docx, .pptx

**Q**: How do I monitor the progress of an upload?<br>
**A**: The best way to monitor progress is by subscribing to the UploadProgressChanged event on your IFileUploadService instance. The server uses SignalR to push real-time updates to the client as chunks are received, so this event will fire frequently during an active upload.
```C#
fileUploadService.UploadProgressChanged += (sender, args) =>
{
    // Update your UI here, e.g., a progress bar
    myProgressBar.Value = args.ProgressPercentage;
    Console.WriteLine($"Progress: {args.ProgressPercentage:F2}%");
};
```
**Q**: How does the server clean up abandoned or failed uploads?<br>
**A**: The API includes a background service (SessionStatusCheckerWorker) that runs periodically (e.g., once every 24 hours). This service queries the database for sessions that have been in an `InProgress` or `Paused` state for an extended period (e.g., more than 48 hours). It marks these abandoned sessions as `Failed` and triggers a cleanup process to delete any associated temporary chunk files from the disk, preventing orphaned files from consuming storage space.

**Q**: How can I customize the chunk size?<br>
**A**: The chunk size is a server-side configuration and cannot be set by the client. This design choice ensures that the server has control over its resource allocation (like memory and temporary storage) and can enforce policies consistently for all clients. An administrator can change the `MaxChunkSize` value in the `appsettings.json` file on the server.

A smaller chunk size (e.g., 1-2 MB) can improve reliability on very unstable networks, as less data needs to be re-transmitted on failure. A larger chunk size (e.g., 5-10 MB) can be more efficient on stable, high-speed networks by reducing the overhead of numerous HTTP requests.

**Q:** What compression algorithms are supported? <br>
**A:** The API supports `GZip`, `Brotli`, and `Deflate`. You can specify your desired algorithm and compression level (e.g., Fastest or Optimal) when configuring the `FileUploadService` using the client SDK's `WithCompressionOption()` method.

**Q:** Can I upload chunks out of order? <br> 
**A:** Yes. The system is designed to handle chunks arriving in any order. Each chunk upload request includes a specific index number (e.g., chunk 5 of 100). The server uses this index to correctly reassemble the file in the final step, regardless of the upload sequence. This allows the client SDK to upload chunks in parallel without waiting for the previous one to complete.

**Q:** What if a session remains incomplete for a long time? <br> 
**A:** Sessions that are inactive for over 48 hours are automatically marked as failed by a background service.

**Q:** Is there a size limit on files?  <br>
**A:** There is no hard-coded limit in the API itself, but the practical maximum file size is determined by the `MaxFileSize` setting in the server's `appsettings.json` file. By default, this is set to 1 GB, but an administrator can increase it. The ultimate limit is also constrained by the available disk space on the server for storing the final merged file.

**Q:** How is data integrity ensured? <br> 
**A:** Currently, data integrity is ensured by tracking the successful reception of all chunks. The server verifies that it has received the correct number of chunks before it will allow a session to be completed. For future enhancements, a more robust mechanism involving checksums (e.g., SHA-256) for each chunk and for the complete file is planned. This would allow the server to verify that the transmitted data has not been corrupted in transit.

**Q:** Can I restart a failed session from the beginning? <br> 
**A:** You cannot "restart" a session that has been marked as `Failed . However, you can simply start a new upload for the same file. This will create a completely new session with a new `SessionId`. The old, failed session will eventually be cleaned up by the server's background service.

**Q:** Is HTTPS required for uploading?  <br>
**A:** While not strictly required and the API can function over HTTP, using HTTPS is strongly recommended for all production environments. Without HTTPS, your file data and API key are transmitted in plaintext over the network, making them vulnerable to interception. The server can and should be configured to enforce HTTPS on its endpoints for secure communication.

**Q:** Can I upload from a mobile or low-bandwidth device?<br>  
**A:** Yes. Chunking and resumable uploads are designed to tolerate unstable or low-bandwidth connections.

**Q:** What happens if two clients try to upload to the same session? <br> 
**A:** Simultaneous uploads are supported as long as chunk indices are coordinated correctly. Otherwise, the server may reject duplicates.

**Q:** How are files stored on disk? <br> 
**A:** During an upload, each chunk is stored as a small, separate temporary file in the TempDirectory specified in the server configuration. Once the upload is complete and finalized, the server reads these temporary files in the correct order and merges them into a single, complete file, which is then saved to the final SavingDirectory. The temporary chunk files are deleted immediately after the merge is successful.



