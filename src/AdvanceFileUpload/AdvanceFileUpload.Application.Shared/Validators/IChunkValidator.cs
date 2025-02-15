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
        bool IsValidateChunkIndex(int chunkIndex);

        /// <summary>
        /// Validates the chunk data.
        /// </summary>
        /// <param name="chunkData">The data of the chunk to validate.</param>
        /// <param name="MaxChunkSize">The maximum size of the chunk.</param>
        /// <returns>True if the chunk data is valid; otherwise, false.</returns>
        bool IsValidateChunkData(byte[] chunkData, long MaxChunkSize);




        ///<summary>
        /// Validates the size of the chunk.
        /// </summary>
        /// <param name="chunkSize">The size of the chunk to validate.</param>
        /// <param name="maxChunkSize">The maximum allowed size of the chunk.</param>
        /// <returns>True if the chunk size is valid; otherwise, false.</returns>
        bool IsValidateChunkSize(long chunkSize, long maxChunkSize);
    }
}
