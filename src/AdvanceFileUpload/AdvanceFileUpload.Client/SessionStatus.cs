using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvanceFileUpload.Client
{
    internal enum SessionStatus
    {
        None,
        Created,
        Uploading,
        Paused,
        Completed,
        Canceled
    }
}
