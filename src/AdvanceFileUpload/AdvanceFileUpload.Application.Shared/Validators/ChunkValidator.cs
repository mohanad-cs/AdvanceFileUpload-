namespace AdvanceFileUpload.Application.Validators
{
    /// <summary>
    /// Represents a chunk validator.
    /// </summary>
    public class ChunkValidator : IChunkValidator
    {
        ///<inheritdoc/>
        public bool IsValidChunkIndex(int chunkIndex)
        {
            return chunkIndex >= 0;
        }
        ///<inheritdoc/>
        public bool IsValidChunkData(byte[] chunkData, long MaxChunkSize)
        {
            return chunkData != null && chunkData.Length > 0 && chunkData.Length <= MaxChunkSize;
        }
        ///<inheritdoc/>
        public bool IsValidChunkSize(long chunkSize, long maxChunkSize)
        {
            return chunkSize > 0 && chunkSize <= maxChunkSize;
        }


    }
}
