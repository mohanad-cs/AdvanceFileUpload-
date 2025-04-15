using AdvanceFileUpload.Application;
using AdvanceFileUpload.Application.Request;
using AdvanceFileUpload.Application.Response;
using AdvanceFileUpload.Application.Shared;
using Microsoft.AspNetCore.Mvc;

namespace AdvanceFileUpload.API.Controllers
{

    [ApiController]
    public class FileUploadController : ControllerBase
    {
        private readonly IUploadManger _uploadManager;

        public FileUploadController(IUploadManger uploadManager)
        {
            _uploadManager = uploadManager ?? throw new ArgumentNullException(nameof(uploadManager));
        }

        /// <summary>
        /// Creates a new file upload session.
        /// </summary>
        /// <param name="request">The request containing the details of the file to be uploaded.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>The response with the details of the created upload session.</returns>
        [HttpPost(RouteTemplates.CreateSession)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<CreateUploadSessionResponse>> CreateUploadSessionAsync([FromBody] CreateUploadSessionRequest request, CancellationToken cancellationToken)
        {
            var response = await _uploadManager.CreateUploadSessionAsync(request, cancellationToken);
            return Ok(response);
        }

        /// <summary>
        /// Completes the file upload session.
        /// </summary>
        /// <param name="request">The unique identifier of the upload session.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>Indicates whether the file upload was completed successfully.</returns>
        [HttpPost(RouteTemplates.CompleteSession)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<bool>> CompleteUploadSessionAsync([FromBody] CompleteUploadSessionRequest request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return BadRequest("CompleteUploadSessionRequest is null ");
            }
            var result = await _uploadManager.CompleteUploadSessionAsync(request.SessionId, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Uploads a chunk of the file.
        /// </summary>
        /// <param name="request">The request containing the details of the chunk to be uploaded.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>Indicates whether the chunk was uploaded successfully.</returns>
        [HttpPost(RouteTemplates.UploadChunk)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<bool>> UploadChunkAsync([FromBody] UploadChunkRequest request, CancellationToken cancellationToken)
        {
            var result = await _uploadManager.UploadChunkAsync(request, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Gets the status of the upload session.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the upload session.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>The response with the status of the upload session.</returns>
        [HttpGet(RouteTemplates.SessionStatus)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<UploadSessionStatusResponse?>> GetUploadSessionStatusAsync([FromQuery] Guid sessionId, CancellationToken cancellationToken)
        {

            var response = await _uploadManager.GetUploadSessionStatusAsync(sessionId, cancellationToken);
            if (response == null)
            {
                return NotFound("The Session do not found");
            }
            return Ok(response);
        }

        /// <summary>
        /// Cancels the file upload session.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>Indicates whether the session was canceled successfully.</returns>
        [HttpPost(RouteTemplates.CancelSession)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<bool>> CancelUploadSessionAsync([FromBody] CancelUploadSessionRequest request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return BadRequest("CancelUploadSessionRequest is null ");
            }
            var result = await _uploadManager.CancelUploadSessionAsync(request.SessionId, cancellationToken);
            return Ok(result);
        }
        /// <summary>
        /// Pauses the file upload session.
        /// </summary>
        /// <param name="sessionId">The unique identifier of the upload session.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>Indicates whether the session was paused successfully.</returns>
        [HttpPost(RouteTemplates.PauseSession)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<bool>> PauseUploadSessionAsync([FromBody] PauseUploadSessionRequest request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return BadRequest("PauseUploadSessionRequest is null ");
            }
            var result = await _uploadManager.PauseUploadSessionAsync(request.SessionId, cancellationToken);
            return Ok(result);
        }
    }
}
