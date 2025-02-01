namespace AdvanceFileUpload.Application.Shared
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
        bool ValidateChunkIndex(int chunkIndex);

        /// <summary>
        /// Validates the chunk data.
        /// </summary>
        /// <param name="chunkData">The data of the chunk to validate.</param>
        /// <param name="MaxChunkSize">The maximum size of the chunk.</param>
        /// <returns>True if the chunk data is valid; otherwise, false.</returns>
        bool ValidateChunkData(byte[] chunkData, long MaxChunkSize);
    }
    /// <summary>
    /// Represents a chunk validator.
    /// </summary>
    public sealed class ChunkValidator : IChunkValidator
    {
        ///<inheritdoc/>
        public bool ValidateChunkIndex(int chunkIndex)
        {
            if (chunkIndex < 0)
            {
                return false;
            }
            return true;
        }
        ///<inheritdoc/>
        public bool ValidateChunkData(byte[] chunkData, long MaxChunkSize)
        {
            if (chunkData == null || chunkData.Length == 0 || chunkData.Length > MaxChunkSize)
            {
                return false;
            }
            return true;
        }
    }
}
