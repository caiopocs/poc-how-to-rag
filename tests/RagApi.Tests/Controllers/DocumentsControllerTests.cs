using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RagApi.Controllers;
using RagApi.Services;
using Xunit;

namespace RagApi.Tests.Controllers
{
    public class DocumentsControllerTests
    {
        private readonly Mock<IDocumentService> _mockDocumentService;
        private readonly DocumentsController _controller;

        public DocumentsControllerTests()
        {
            _mockDocumentService = new Mock<IDocumentService>();
            _controller = new DocumentsController(_mockDocumentService.Object);
        }

        [Fact]
        public async Task IngestDocument_WithValidFile_ReturnsOk()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            var content = "Test content";
            var fileName = "test.txt";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            await writer.WriteAsync(content);
            await writer.FlushAsync();
            ms.Position = 0;

            mockFile.Setup(f => f.FileName).Returns(fileName);
            mockFile.Setup(f => f.Length).Returns(ms.Length);
            mockFile.Setup(f => f.OpenReadStream()).Returns(ms);

            _mockDocumentService
                .Setup(s => s.IngestDocumentAsync(It.IsAny<Stream>(), fileName))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.IngestDocument(mockFile.Object);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _mockDocumentService.Verify(s => 
                s.IngestDocumentAsync(It.IsAny<Stream>(), fileName), Times.Once);
        }

        [Fact]
        public async Task IngestDocument_WithNullFile_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.IngestDocument(null!);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task IngestDocument_WithEmptyFile_ReturnsBadRequest()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.Length).Returns(0);

            // Act
            var result = await _controller.IngestDocument(mockFile.Object);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task IngestDocument_WhenServiceFails_ReturnsInternalServerError()
        {
            // Arrange
            var mockFile = new Mock<IFormFile>();
            var ms = new MemoryStream(new byte[] { 1, 2, 3 });
            
            mockFile.Setup(f => f.FileName).Returns("test.txt");
            mockFile.Setup(f => f.Length).Returns(3);
            mockFile.Setup(f => f.OpenReadStream()).Returns(ms);

            _mockDocumentService
                .Setup(s => s.IngestDocumentAsync(It.IsAny<Stream>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.IngestDocument(mockFile.Object);

            // Assert
            var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
            statusCodeResult.StatusCode.Should().Be(500);
        }
    }
}