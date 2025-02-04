namespace AdvanceFileUpload.Application.Shared
{
    /// <summary>
    /// Represents a file validator.
    /// </summary>
    public sealed class FileValidator : IFileValidator
    {
        ///<inheritdoc/>
        public bool ValidateFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }
            return true;
        }
        ///<inheritdoc/>
        public bool ValidateFileSize(long fileSize, long maxFileSize)
        {
            if (fileSize <= 0 || fileSize>maxFileSize)
            {
                return false;
            }
            return true;
        }
        ///<inheritdoc/>
        public bool ValidateFileExtension(string fileExtension , string[] allowedExtensions)
        {
            if (string.IsNullOrWhiteSpace(fileExtension))
            {
                return false;
            }
            if (allowedExtensions == null || allowedExtensions.Length == 0)
            {
                return false;
            }
            if (!allowedExtensions.Contains(fileExtension))
            {
                return false;
            }
            return true;
        }
    }
}
