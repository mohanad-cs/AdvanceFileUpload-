using AdvanceFileUpload.Domain.Core;

namespace AdvanceFileUpload.Domain.Exceptions
{
    /// <summary>
    /// Represents errors that occur during the pausing of a file upload.
    /// </summary>
    public class PauseFileUploadException : DomainException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PauseFileUploadException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public PauseFileUploadException(string? message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PauseFileUploadException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public PauseFileUploadException(string? message, System.Exception? innerException) : base(message, innerException)
        {
        }
    }
}
