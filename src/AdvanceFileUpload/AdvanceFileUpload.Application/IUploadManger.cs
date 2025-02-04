using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdvanceFileUpload.Application.Shared;
using AdvanceFileUpload.Application.Exception;

namespace AdvanceFileUpload.Application
{
    /// <summary>
    /// Interface for managing file upload sessions.
    /// </summary>
    public interface IUploadManger
    {
        /// <summary>
        /// Creates a new file upload session asynchronously.
        /// </summary>
        /// <param name="request">The request containing the details of the file to be uploaded.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response with the details of the created upload session.</returns>
        Task<CreateUploadSessionResponse> CreateUploadSessionAsync(CreateUploadSessionRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Completes the file upload session asynchronously.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the upload session.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the File was Uploading Completed successfully.</returns>
        Task<bool> CompleteUploadSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads a chunk of the file asynchronously.
        /// </summary>
        /// <param name="request">The request containing the details of the chunk to be uploaded.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the chunk was uploaded successfully.</returns>
        Task<bool> UploadChunkAsync(UploadChunkRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the status of the upload session asynchronously.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the upload session.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response with the status of the upload session.</returns>
        Task<UploadSessionStatusResponse?> GetUploadSessionStatusAsync(Guid sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cancels the file upload session asynchronously.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the upload session.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the session was canceled successfully.</returns>
        Task<bool> CancelUploadSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);

        /// <summary>
        ///  pause the file upload session asynchronously.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the upload session.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the session was paused successfully.</returns>
        Task<bool> PauseUploadSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    }
}
