namespace AdvanceFileUpload.Application.Validators
{
    /// <summary>
    /// Represents a chunk validator.
    /// </summary>
    public  class ChunkValidator : IChunkValidator
    {
        ///<inheritdoc/>
        public bool IsValidateChunkIndex(int chunkIndex)
        {
            if (chunkIndex < 0)
            {
                return false;
            }
            return true;
        }
        ///<inheritdoc/>
        public bool IsValidateChunkData(byte[] chunkData, long MaxChunkSize)
        {
            if (chunkData == null || chunkData.Length == 0 || chunkData.Length > MaxChunkSize)
            {
                return false;
            }
            return true;
        }
        ///<inheritdoc/>

        public bool IsValidateChunkSize(long chunkSize, long maxChunkSize)
        {
            return  chunkSize > 0 || chunkSize <= maxChunkSize;

        }
    }
}
