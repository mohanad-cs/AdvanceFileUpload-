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
        public const string CreateSession = $"{Base}create-session";

        /// <summary>
        /// The route for completing an upload session.
        /// </summary>
        /// <remarks>
        /// Requires a session ID.
        /// </remarks>
        public const string CompleteSession = $"{Base}complete-session";

        /// <summary>
        /// The route for uploading a chunk of a file.
        /// </summary>
        public const string UploadChunk = $"{Base}upload-chunk";

        /// <summary>
        /// The route for checking the status of an upload session.
        /// </summary>
        /// <remarks>
        /// Requires a session ID.
        /// </remarks>
        public const string SessionStatus = $"{Base}session-status";

        /// <summary>
        /// The route for canceling an upload session.
        /// </summary>
        /// <remarks>
        /// Requires a session ID.
        /// </remarks>
        public const string CancelSession = $"{Base}cancel-session";

        /// <summary>
        /// The route for pausing an upload session.
        /// </summary>
        /// <remarks>
        /// Requires a session ID.
        /// </remarks>
        public const string PauseSession = $"{Base}pause-session";

        /// <summary>
        /// The route for the upload process hub.
        /// </summary>
        public const string UploadProcessHub = "api/UploadProcessHub";
    }


}
