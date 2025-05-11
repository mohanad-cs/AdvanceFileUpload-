using AdvanceFileUpload.Application.Compression;
using AdvanceFileUpload.Application.Validators;

namespace AdvanceFileUpload.Domain.Test
{
    public class FileCompressionValidatorTests
    {
        private readonly FileCompressionValidator _validator;

        public FileCompressionValidatorTests()
        {
            _validator = new FileCompressionValidator();
        }

        //[Theory]
        //[InlineData(new byte[] { 0x1F, 0x8B }, CompressionAlgorithmOption.GZip, true)]
        //[InlineData(new byte[] { 0x1F }, CompressionAlgorithmOption.GZip, false)]
        //[InlineData(new byte[] { 0x78, 0x9C }, CompressionAlgorithmOption.Deflate, true)]
        //[InlineData(new byte[] { 0x78 }, CompressionAlgorithmOption.Deflate, false)]
        //[InlineData(new byte[] { 0xCE, 0xB2, 0xCF, 0x81 }, CompressionAlgorithmOption.Brotli, true)]
        //[InlineData(new byte[] { 0xCE, 0xB2, 0xCF }, CompressionAlgorithmOption.Brotli, false)]
        //public void IsValidCompressedData_ShouldReturnExpectedResult(byte[] data, CompressionAlgorithmOption option, bool expected)
        //{
        //    var result = _validator.IsValidCompressedData(data, option);
        //    Assert.Equal(expected, result);
        //}

        [Fact]
        public void IsValidCompressedData_ShouldThrowArgumentException_ForUnsupportedAlgorithm()
        {
            var data = new byte[] { 0x00, 0x00 };
            Assert.Throws<ArgumentException>(() => _validator.IsValidCompressedData(data, (CompressionAlgorithmOption)999));
        }
    }
}
