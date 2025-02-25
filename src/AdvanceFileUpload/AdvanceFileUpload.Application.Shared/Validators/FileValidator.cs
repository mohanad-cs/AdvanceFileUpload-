namespace AdvanceFileUpload.Application.Validators
{
    /// <summary>
    /// Represents a file validator.
    /// </summary>
    public  class FileValidator : IFileValidator
    {
        // a list of invalid file names characters
        private static readonly char[] _invalidFileNameChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];
        ///<inheritdoc/>
        public bool IsValidFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }
            if (fileName.IndexOfAny(_invalidFileNameChars) >= 0)
            {
                return false;
            }
            return true;
        }
        ///<inheritdoc/>
        public bool IsValidFileSize(long fileSize, long maxFileSize)
        {
           return fileSize>0 && fileSize <= maxFileSize;
        }
        ///<inheritdoc/>
        public bool IsValidFileExtension(string fileExtension, string[] allowedExtensions)
        {
            if (string.IsNullOrWhiteSpace(fileExtension) || allowedExtensions == null || allowedExtensions.Length == 0)
            {
                return false;
            }
            return Array.Exists(allowedExtensions, extension => extension.Equals(fileExtension, StringComparison.OrdinalIgnoreCase));
        }
    }
}
