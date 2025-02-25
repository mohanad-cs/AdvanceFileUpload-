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
            return chunkIndex >= 0;
        }
        ///<inheritdoc/>
        public bool IsValidateChunkData(byte[] chunkData, long MaxChunkSize)
        {
            return chunkData != null && chunkData.Length > 0 && chunkData.Length <= MaxChunkSize;
        }
        ///<inheritdoc/>

        public bool IsValidateChunkSize(long chunkSize, long maxChunkSize)
        {
            return chunkSize > 0 && chunkSize <= maxChunkSize;
        }
    }
}
