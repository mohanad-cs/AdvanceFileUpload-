using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace AdvanceFileUpload.Application.Shared
{
    /// <summary>
    /// Provides methods for file operations such as concatenating file chunks, saving files, and splitting files into chunks.
    /// </summary>
    public interface IFileOperationService
    {

        /// <summary>
        /// Concatenates multiple file chunks into a single file.
        /// </summary>
        /// <param name="chunkPaths">The list of paths to the file chunks.</param>
        /// <param name="outputFilePath">The path where the concatenated file will be saved.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task ConcatenateChunksAsync(List<string> chunkPaths, string outputFilePath , CancellationToken cancellationToken=default);
        /// <summary>
        /// Saves a file to the specified directory.
        /// </summary>
        /// <param name="fileName">The name of the file to save.</param>
        /// <param name="fileData">The byte array containing the file data.</param>
        /// <param name="directory">The directory where the file will be saved.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task SaveFileAsync(string fileName, byte[] fileData, string directory , CancellationToken cancellationToken =default);
        /// <summary>
        /// Splits a file into multiple chunks.
        /// </summary>
        /// <param name="filePath">The path to the file to be split.</param>
        /// <param name="chunkSize">The size of each chunk in bytes.</param>
        /// <param name="outputDirectory">The directory where the chunks will be saved.</param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of paths to the created chunks.</returns>
        Task<List<string>> SplitFileIntoChunksAsync(string filePath, long chunkSize, string outputDirectory ,CancellationToken cancellationToken=default);
    }
}
