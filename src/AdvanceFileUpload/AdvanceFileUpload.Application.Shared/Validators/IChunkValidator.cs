using AdvanceFileUpload.Application.Compression;

namespace AdvanceFileUpload.Application.Validators
{

    ///<summary>
    /// Interface for validating file chunks.
    /// </summary>
    public interface IChunkValidator
    {
        /// <summary>
        /// Validates the chunk index.
        /// </summary>
        /// <param name="chunkIndex">The index of the chunk to validate.</param>
        /// <returns>True if the chunk index is valid; otherwise, false.</returns>
        bool IsValidChunkIndex(int chunkIndex);

        /// <summary>
        /// Validates the chunk data.
        /// </summary>
        /// <param name="chunkData">The data of the chunk to validate.</param>
        /// <param name="MaxChunkSize">The maximum size of the chunk.</param>
        /// <returns>True if the chunk data is valid; otherwise, false.</returns>
        bool IsValidChunkData(byte[] chunkData, long MaxChunkSize);

        ///<summary>
        /// Validates the size of the chunk.
        /// </summary>
        /// <param name="chunkSize">The size of the chunk to validate.</param>
        /// <param name="maxChunkSize">The maximum allowed size of the chunk.</param>
        /// <returns>True if the chunk size is valid; otherwise, false.</returns>
        bool IsValidChunkSize(long chunkSize, long maxChunkSize);
        /// <summary>
        /// Validates the compressed chunk data based on the specified compression algorithm.
        /// </summary>
        /// <param name="chunkData">The data of the chunk to validate.</param>
        /// <param name="compressionAlgorithmOption">The compression algorithm used for the chunk data.</param>
        /// <returns>True if the compressed chunk data is valid; otherwise, false.</returns>
        bool IsValidCompressedChunkData(byte[] chunkData, CompressionAlgorithmOption compressionAlgorithmOption);
    }
}
