using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AdvanceFileUpload.Client
{
    public class UploadException : Exception
    {
       
        public UploadException(string? message) : base(message)
        {
        }

        public UploadException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected UploadException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
