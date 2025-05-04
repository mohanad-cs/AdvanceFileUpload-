namespace AdvanceFileUpload.API
{
    public sealed class ThreadPoolOptions
    {
        public const string SectionName = "ThreadPoolOptions";
        public int MinThreads { get; set; } = 100;
        public int MaxThreads { get; set; } = 100;
    }
}
