using AdvanceFileUpload.Domain.Core;

namespace AdvanceFileUpload.Domain.Exceptions
{
    /// <summary>
    /// Represents errors that occur during the cancellation of a file upload.
    /// </summary>
    public class CancelationFileUploadException : DomainException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CancelationFileUploadException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public CancelationFileUploadException(string? message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CancelationFileUploadException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public CancelationFileUploadException(string? message, System.Exception? innerException) : base(message, innerException)
        {
        }
    }
}
