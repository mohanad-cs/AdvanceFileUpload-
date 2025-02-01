using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace AdvanceFileUpload.Application.Exception
{
    public sealed class ApplicationException : System.Exception
    {
       
        public ApplicationException(string? message) : base(message)
        {
        }

        public ApplicationException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public ApplicationException(string? message, System.Exception? innerException) : base(message, innerException)
        {
        }
    }
}
