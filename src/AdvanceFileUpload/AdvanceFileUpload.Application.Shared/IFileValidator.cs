namespace AdvanceFileUpload.Application.Shared
{

    /// <summary>
    /// Interface for validating files.
    /// </summary>
    public interface IFileValidator
    {
        /// <summary>
        /// Validates the file name.
        /// </summary>
        /// <param name="fileName">The name of the file to validate.</param>
        /// <returns>True if the file name is valid; otherwise, false.</returns>
        bool ValidateFileName(string fileName);

        /// <summary>
        /// Validates the file size.
        /// </summary>
        /// <param name="fileSize">The size of the file to validate.</param>
        /// <param name="maxFileSize">The maximum allowed size of the file.</param>
        /// <returns>True if the file size is valid; otherwise, false.</returns>
        bool ValidateFileSize(long fileSize, long maxFileSize);

        /// <summary>
        /// Validates the file extension.
        /// </summary>
        /// <param name="fileExtension">The extension of the file to validate.</param>
        /// <param name="allowedExtensions">The allowed extensions of the file to validate.</param>
        /// <returns>True if the file extension is valid; otherwise, false.</returns>
        bool ValidateFileExtension(string fileExtension , string[] allowedExtensions);
    }

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
