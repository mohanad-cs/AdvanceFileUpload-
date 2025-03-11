using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvanceFileUpload.Application.Shared
{

    
    ///<summary>
    /// Contains route templates for the file upload API.
    /// </summary>
    public static class RouteTemplates
    {
        /// <summary>
        /// The base route for the file upload API.
        /// </summary>
        public const string Base = "api/upload/";

        /// <summary>
        /// The route for creating a new upload session.
        /// </summary>
        public const string CreateSession ="create-session";

        /// <summary>
        /// The route for completing an upload session.
        /// </summary>
        /// <remarks>
        /// Requires a session ID.
        /// </remarks>
        public const string CompleteSession ="complete-session/";

        /// <summary>
        /// The route for uploading a chunk of a file.
        /// </summary>
        public const string UploadChunk = "upload-chunk";

        /// <summary>
        /// The route for checking the status of an upload session.
        /// </summary>
        /// <remarks>
        /// Requires a session ID.
        /// </remarks>
        public const string SessionStatus = "session-status/";

        /// <summary>
        /// The route for canceling an upload session.
        /// </summary>
        /// <remarks>
        /// Requires a session ID.
        /// </remarks>
        public const string CancelSession = "cancel-session/";

        /// <summary>
        /// The route for pausing an upload session.
        /// </summary>
        /// <remarks>
        /// Requires a session ID.
        /// </remarks>
        public const string PauseSession = "pause-session/";
    }
    

}
