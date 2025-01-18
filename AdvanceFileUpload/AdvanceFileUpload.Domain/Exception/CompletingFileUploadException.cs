using AdvanceFileUpload.Domain.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvanceFileUpload.Domain.Exception
{
    /// <summary>
    /// Represents errors that occur during the completion of a file upload.
    /// </summary>
    public class CompletingFileUploadException : DomainException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompletingFileUploadException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public CompletingFileUploadException(string? message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompletingFileUploadException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public CompletingFileUploadException(string? message, System.Exception? innerException) : base(message, innerException)
        {
        }
    }
}
