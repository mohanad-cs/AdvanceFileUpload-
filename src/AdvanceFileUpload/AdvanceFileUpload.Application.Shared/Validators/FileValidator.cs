namespace AdvanceFileUpload.Application.Validators
{
    /// <summary>
    /// Represents a file validator.
    /// </summary>
    public  class FileValidator : IFileValidator
    {
        ///<inheritdoc/>
        public bool IsValidateFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return false;
            }
            return true;
        }
        ///<inheritdoc/>
        public bool IsValidateFileSize(long fileSize, long maxFileSize)
        {
            if (fileSize <= 0 || fileSize > maxFileSize)
            {
                return false;
            }
            return true;
        }
        ///<inheritdoc/>
        public bool IsValidateFileExtension(string fileExtension, string[] allowedExtensions)
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
