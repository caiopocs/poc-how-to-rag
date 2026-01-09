using FluentAssertions;
using Microsoft.KernelMemory;
using Moq;
using RagApi.Models;
using RagApi.Services;
using Xunit;

namespace RagApi.Tests.Services
{
    public class ChatServiceTests
    {
        private readonly Mock<IKernelMemory> _mockMemory;
        private readonly ChatService _service;

        public ChatServiceTests()
        {
            _mockMemory = new Mock<IKernelMemory>();
            _service = new ChatService(_mockMemory.Object);
        }

        [Fact]
        public async Task AskQuestionAsync_WithNullQuestion_ReturnsEmptyResponse()
        {
            // Act
            var result = await _service.AskQuestionAsync(null!);

            // Assert
            result.Should().NotBeNull();
            result.Answer.Should().BeEmpty();
            result.Sources.Should().BeEmpty();
        }

        [Fact]
        public async Task AskQuestionAsync_WithEmptyQuestion_ReturnsEmptyResponse()
        {
            // Act
            var result = await _service.AskQuestionAsync(string.Empty);

            // Assert
            result.Should().NotBeNull();
            result.Answer.Should().BeEmpty();
            result.Sources.Should().BeEmpty();
        }

        [Fact]
        public async Task AskQuestionAsync_WithWhitespaceQuestion_ReturnsEmptyResponse()
        {
            // Act
            var result = await _service.AskQuestionAsync("   ");

            // Assert
            result.Should().NotBeNull();
            result.Answer.Should().BeEmpty();
            result.Sources.Should().BeEmpty();
        }
    }
}