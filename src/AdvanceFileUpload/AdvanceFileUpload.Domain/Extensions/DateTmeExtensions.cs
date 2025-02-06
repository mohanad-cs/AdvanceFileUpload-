namespace AdvanceFileUpload.Domain.Extensions
{
    /// <summary>
    /// Provides extension methods for <see cref="DateTime"/> objects.
    /// </summary>
    public static class DateTmeExtensions
    {
        /// <summary>
        /// Converts a <see cref="DateTime"/> object to a <see cref="DateOnly"/> object.
        /// </summary>
        /// <param name="dateTime">The <see cref="DateTime"/> object to convert.</param>
        /// <returns>A <see cref="DateOnly"/> object representing the date component of the <see cref="DateTime"/> object.</returns>
        public static DateOnly ToDateOnly(this DateTime dateTime)
        {
            return DateOnly.FromDateTime(dateTime);
        }
    }
}
