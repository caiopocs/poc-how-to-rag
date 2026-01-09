using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using RagApi.Controllers;
using RagApi.Models;
using RagApi.Services;
using Xunit;

namespace RagApi.Tests.Controllers
{
    public class ChatControllerTests
    {
        private readonly Mock<IChatService> _mockChatService;
        private readonly ChatController _controller;

        public ChatControllerTests()
        {
            _mockChatService = new Mock<IChatService>();
            _controller = new ChatController(_mockChatService.Object);
        }

        [Fact]
        public async Task AskQuestion_WithValidRequest_ReturnsOkWithResponse()
        {
            // Arrange
            var request = new ChatRequest { Question = "What is RAG?" };
            var expectedResponse = new ChatResponse 
            { 
                Answer = "RAG stands for Retrieval-Augmented Generation.",
                Sources = new List<string> { "doc1.pdf" }
            };

            _mockChatService
                .Setup(s => s.AskQuestionAsync(request.Question))
                .ReturnsAsync(expectedResponse);

            // Act
            var result = await _controller.AskQuestion(request);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task AskQuestion_WithNullRequest_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.AskQuestion(null!);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task AskQuestion_WithEmptyQuestion_ReturnsBadRequest()
        {
            // Arrange
            var request = new ChatRequest { Question = "" };

            // Act
            var result = await _controller.AskQuestion(request);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task AskQuestion_WithWhitespaceQuestion_ReturnsBadRequest()
        {
            // Arrange
            var request = new ChatRequest { Question = "   " };

            // Act
            var result = await _controller.AskQuestion(request);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}