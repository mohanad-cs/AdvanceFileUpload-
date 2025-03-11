using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvanceFileUpload.Client
{
    public interface IFileUploadService
    {

        Task UploadFileAsync(string filePath, CancellationToken cancellationToken);
        Task PauseUploadAsync();
        Task ResumeUploadAsync(CancellationToken cancellationToken);
    }
}
