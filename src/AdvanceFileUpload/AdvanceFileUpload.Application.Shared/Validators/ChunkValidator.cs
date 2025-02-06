namespace AdvanceFileUpload.Application.Validators
{
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
        ///<inheritdoc/>

        public bool ValidateChunkSize(long chunkSize, long maxChunkSize)
        {
            return chunkSize <= 0 || chunkSize > maxChunkSize;

        }
    }
}
