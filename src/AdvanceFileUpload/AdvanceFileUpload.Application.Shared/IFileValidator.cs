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
}
