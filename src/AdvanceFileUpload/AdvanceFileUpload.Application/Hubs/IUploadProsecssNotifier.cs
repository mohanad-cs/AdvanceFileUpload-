using AdvanceFileUpload.Application.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvanceFileUpload.Application.Hubs
{
    public interface IUploadProcessNotifier
    {
        Task NotifyUploadProgressAsync(string? connectionId , UploadSessionStatusNotification uploadSessionStatusNotification, CancellationToken cancellationToken =default );
      
    }
}
