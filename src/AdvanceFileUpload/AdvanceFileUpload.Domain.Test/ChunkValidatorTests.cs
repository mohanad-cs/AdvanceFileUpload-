using AdvanceFileUpload.Application.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvanceFileUpload.Domain.Test
{
    public class ChunkValidatorTests
    {
        private readonly ChunkValidator _chunkValidator;

        public ChunkValidatorTests()
        {
            _chunkValidator = new ChunkValidator();
        }

        [Fact]
        public void IsValidateChunkIndex_ValidIndex_ReturnsTrue()
        {
            // Arrange
            int validIndex = 1;

            // Act
            bool result = _chunkValidator.IsValidateChunkIndex(validIndex);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidateChunkIndex_InvalidIndex_ReturnsFalse()
        {
            // Arrange
            int invalidIndex = -1;
            // Act
            bool result = _chunkValidator.IsValidateChunkIndex(invalidIndex);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidateChunkData_ValidData_ReturnsTrue()
        {
            // Arrange
            byte[] validData = new byte[] { 1, 2, 3 };
            long maxChunkSize = 10;

            // Act
            bool result = _chunkValidator.IsValidateChunkData(validData, maxChunkSize);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidateChunkData_NullData_ReturnsFalse()
        {
            // Arrange
            byte[] nullData = null;
            long maxChunkSize = 10;

            // Act
            bool result = _chunkValidator.IsValidateChunkData(nullData, maxChunkSize);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidateChunkData_EmptyData_ReturnsFalse()
        {
            // Arrange
            byte[] emptyData = new byte[] { };
            long maxChunkSize = 10;

            // Act
            bool result = _chunkValidator.IsValidateChunkData(emptyData, maxChunkSize);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidateChunkData_ExceedsMaxSize_ReturnsFalse()
        {
            // Arrange
            byte[] largeData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };
            long maxChunkSize = 10;

            // Act
            bool result = _chunkValidator.IsValidateChunkData(largeData, maxChunkSize);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidateChunkSize_ValidSize_ReturnsTrue()
        {
            // Arrange
            long validSize = 5;
            long maxChunkSize = 10;

            // Act
            bool result = _chunkValidator.IsValidateChunkSize(validSize, maxChunkSize);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidateChunkSize_ZeroSize_ReturnsFalse()
        {
            // Arrange
            long zeroSize = 0;
            long maxChunkSize = 10;

            // Act
            bool result = _chunkValidator.IsValidateChunkSize(zeroSize, maxChunkSize);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidateChunkSize_ExceedsMaxSize_ReturnsFalse()
        {
            // Arrange
            long largeSize = 15;
            long maxChunkSize = 10;

            // Act
            bool result = _chunkValidator.IsValidateChunkSize(largeSize, maxChunkSize);

            // Assert
            Assert.False(result);
        }
    }
}
