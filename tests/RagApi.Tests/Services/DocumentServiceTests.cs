using FluentAssertions;
using Microsoft.KernelMemory;
using Moq;
using RagApi.Services;
using Xunit;

namespace RagApi.Tests.Services
{
    public class DocumentServiceTests
    {
        private readonly Mock<IKernelMemory> _mockMemory;
        private readonly DocumentService _service;

        public DocumentServiceTests()
        {
            _mockMemory = new Mock<IKernelMemory>();
            _service = new DocumentService(_mockMemory.Object);
        }

        [Fact]
        public async Task IngestDocumentAsync_WithNullStream_ReturnsFalse()
        {
            // Act
            var result = await _service.IngestDocumentAsync(null!, "test.txt");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IngestDocumentAsync_WithEmptyFileName_ReturnsFalse()
        {
            // Arrange
            using var stream = new MemoryStream();

            // Act
            var result = await _service.IngestDocumentAsync(stream, string.Empty);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IngestDocumentAsync_WithNullFileName_ReturnsFalse()
        {
            // Arrange
            using var stream = new MemoryStream();

            // Act
            var result = await _service.IngestDocumentAsync(stream, null!);

            // Assert
            result.Should().BeFalse();
        }
    }
}