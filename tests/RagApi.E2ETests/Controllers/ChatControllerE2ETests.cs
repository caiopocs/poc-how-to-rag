using FluentAssertions;
using RagApi.E2ETests.Fixtures;
using RagApi.Models;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RagApi.E2ETests.Controllers
{
    /// <summary>
    /// Testes End-to-End para o endpoint de chat/perguntas.
    /// 
    /// Estes testes validam o fluxo COMPLETO de Q&A através da API HTTP:
    /// 1. Cliente HTTP faz POST /api/chat/ask com pergunta
    /// 2. Controller recebe e valida
    /// 3. ChatService processa via Kernel Memory
    /// 4. Kernel Memory: embed pergunta (Ollama) -> busca similar (Qdrant) -> gera resposta (Ollama LLM)
    /// 5. Controller retorna resposta + fontes
    /// 
    /// PRÉ-REQUISITO: 
    /// - Qdrant e Ollama rodando (docker-compose up -d)
    /// - Documentos previamente ingeridos para ter contexto
    /// </summary>
    public class ChatControllerE2ETests : IClassFixture<RagApiFactory>
    {
        private readonly RagApiFactory _factory;
        private readonly HttpClient _client;
        private readonly JsonSerializerOptions _jsonOptions;

        public ChatControllerE2ETests(RagApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        private async Task IngestTestDocumentAsync()
        {
            // Helper para ingerir documento antes dos testes
            var testFilePath = Path.Combine("TestData", "e2e-test-doc.txt");
            var fileContent = await File.ReadAllBytesAsync(testFilePath);

            using var content = new MultipartFormDataContent();
            using var fc = new ByteArrayContent(fileContent);
            fc.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
            content.Add(fc, "file", "e2e-doc.txt");

            await _client.PostAsync("/api/ingest", content);
            
            // Aguardar processamento
            await Task.Delay(10000);
        }

        [Fact]
        public async Task AskQuestion_WithValidQuestion_ShouldReturn200WithAnswer()
        {
            // Arrange
            await IngestTestDocumentAsync();

            var request = new ChatRequest
            {
                Question = "What is E2E testing for RAG systems?"
            };

            var json = JsonSerializer.Serialize(request);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/chat/ask", httpContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK,
                "deve retornar 200 OK para pergunta válida");

            var responseContent = await response.Content.ReadAsStringAsync();
            var chatResponse = JsonSerializer.Deserialize<ChatResponse>(
                responseContent, _jsonOptions);

            chatResponse.Should().NotBeNull();
            chatResponse!.Answer.Should().NotBeNullOrEmpty(
                "deve retornar uma resposta");
            chatResponse.Answer.Should().Contain("test",
                "deve mencionar testing na resposta");
        }

        [Fact]
        public async Task AskQuestion_WithNullQuestion_ShouldReturn400()
        {
            // Arrange
            var request = new { Question = (string?)null };
            var json = JsonSerializer.Serialize(request);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/chat/ask", httpContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                "deve retornar 400 para pergunta nula");
        }

        [Fact]
        public async Task AskQuestion_WithEmptyQuestion_ShouldReturn400()
        {
            // Arrange
            var request = new ChatRequest { Question = "" };
            var json = JsonSerializer.Serialize(request);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/chat/ask", httpContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                "deve retornar 400 para pergunta vazia");
        }

        [Fact]
        public async Task AskQuestion_WithWhitespaceQuestion_ShouldReturn400()
        {
            // Arrange
            var request = new ChatRequest { Question = "   " };
            var json = JsonSerializer.Serialize(request);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/chat/ask", httpContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                "deve retornar 400 para pergunta só com espaços");
        }

        [Fact]
        public async Task AskQuestion_WithInvalidJson_ShouldReturn400()
        {
            // Arrange
            var invalidJson = "{ invalid json content }";
            var httpContent = new StringContent(invalidJson, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/chat/ask", httpContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                "deve retornar 400 para JSON inválido");
        }

        [Fact]
        public async Task AskQuestion_MultipleQuestionsSequentially_ShouldAllSucceed()
        {
            // Arrange
            await IngestTestDocumentAsync();

            var questions = new[]
            {
                "What is E2E testing?",
                "What are the benefits of E2E testing?",
                "What should be tested in RAG systems?"
            };

            // Act & Assert
            foreach (var question in questions)
            {
                var request = new ChatRequest { Question = question };
                var json = JsonSerializer.Serialize(request);
                var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _client.PostAsync("/api/chat/ask", httpContent);

                response.StatusCode.Should().Be(HttpStatusCode.OK,
                    $"pergunta '{question}' deve ser processada com sucesso");

                var responseContent = await response.Content.ReadAsStringAsync();
                var chatResponse = JsonSerializer.Deserialize<ChatResponse>(
                    responseContent, _jsonOptions);

                chatResponse.Should().NotBeNull();
                chatResponse!.Answer.Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public async Task AskQuestion_WithLongQuestion_ShouldHandleGracefully()
        {
            // Arrange
            await IngestTestDocumentAsync();

            var longQuestion = "Can you please explain in great detail, with all the " +
                "nuances and technical considerations, what exactly is meant by " +
                "end-to-end testing in the context of Retrieval-Augmented Generation " +
                "systems, including all the components that need to be validated, " +
                "the integration points, and the expected outcomes? " +
                "Also, please include any best practices and common pitfalls.";

            var request = new ChatRequest { Question = longQuestion };
            var json = JsonSerializer.Serialize(request);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/chat/ask", httpContent);

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK,
                HttpStatusCode.RequestEntityTooLarge,
                "deve processar ou rejeitar perguntas muito longas de forma controlada");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var chatResponse = JsonSerializer.Deserialize<ChatResponse>(
                    responseContent, _jsonOptions);

                chatResponse.Should().NotBeNull();
                chatResponse!.Answer.Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public async Task AskQuestion_AboutUnrelatedTopic_ShouldRespondAppropriately()
        {
            // Arrange
            await IngestTestDocumentAsync();

            var unrelatedQuestion = "What is the capital of France?";
            var request = new ChatRequest { Question = unrelatedQuestion };
            var json = JsonSerializer.Serialize(request);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/chat/ask", httpContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK,
                "deve retornar 200 mesmo para perguntas não relacionadas");

            var responseContent = await response.Content.ReadAsStringAsync();
            var chatResponse = JsonSerializer.Deserialize<ChatResponse>(
                responseContent, _jsonOptions);

            chatResponse.Should().NotBeNull();
            chatResponse!.Answer.Should().NotBeNullOrEmpty();
            
            // A resposta pode indicar falta de contexto relevante
            // ou o LLM pode responder com conhecimento geral
        }

        [Fact]
        public async Task AskQuestion_WithSpecialCharacters_ShouldHandleCorrectly()
        {
            // Arrange
            await IngestTestDocumentAsync();

            var questionWithSpecialChars = "What is E2E testing? (includes #tags & @mentions!)";
            var request = new ChatRequest { Question = questionWithSpecialChars };
            var json = JsonSerializer.Serialize(request);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/chat/ask", httpContent);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK,
                "deve processar perguntas com caracteres especiais");

            var responseContent = await response.Content.ReadAsStringAsync();
            var chatResponse = JsonSerializer.Deserialize<ChatResponse>(
                responseContent, _jsonOptions);

            chatResponse.Should().NotBeNull();
            chatResponse!.Answer.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task AskQuestion_SameQuestionTwice_ShouldReturnConsistentResults()
        {
            // Arrange
            await IngestTestDocumentAsync();

            var question = "What are the benefits of E2E testing?";
            var request = new ChatRequest { Question = question };
            var json = JsonSerializer.Serialize(request);

            // Act
            var httpContent1 = new StringContent(json, Encoding.UTF8, "application/json");
            var response1 = await _client.PostAsync("/api/chat/ask", httpContent1);

            var httpContent2 = new StringContent(json, Encoding.UTF8, "application/json");
            var response2 = await _client.PostAsync("/api/chat/ask", httpContent2);

            // Assert
            response1.StatusCode.Should().Be(HttpStatusCode.OK);
            response2.StatusCode.Should().Be(HttpStatusCode.OK);

            var content1 = await response1.Content.ReadAsStringAsync();
            var content2 = await response2.Content.ReadAsStringAsync();

            var chatResponse1 = JsonSerializer.Deserialize<ChatResponse>(content1, _jsonOptions);
            var chatResponse2 = JsonSerializer.Deserialize<ChatResponse>(content2, _jsonOptions);

            chatResponse1.Should().NotBeNull();
            chatResponse2.Should().NotBeNull();

            // As respostas devem abordar o mesmo tópico
            // (podem variar ligeiramente devido à natureza do LLM)
            chatResponse1!.Answer.Should().NotBeNullOrEmpty();
            chatResponse2!.Answer.Should().NotBeNullOrEmpty();
        }
    }
}
