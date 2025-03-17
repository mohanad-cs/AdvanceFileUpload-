namespace AdvanceFileUpload.Client
{
    public interface IFileUploadService
    {

        Task UploadFileAsync(string filePath);
        Task PauseUploadAsync();
        Task ResumeUploadAsync();
        Task CancelUploadAsync();
    }
}
