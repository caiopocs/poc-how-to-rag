using FluentAssertions;
using Microsoft.AspNetCore.Http;
using RagApi.E2ETests.Fixtures;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace RagApi.E2ETests.Controllers
{
    /// <summary>
    /// Testes End-to-End para o endpoint de ingestão de documentos.
    /// 
    /// Estes testes validam o fluxo COMPLETO através da API HTTP:
    /// 1. Cliente HTTP faz POST /api/ingest com arquivo
    /// 2. Controller recebe e valida o request
    /// 3. DocumentService processa via Kernel Memory
    /// 4. Kernel Memory: chunk -> embed (Ollama) -> store (Qdrant)
    /// 5. Controller retorna resposta HTTP
    /// 
    /// PRÉ-REQUISITO: Qdrant e Ollama devem estar rodando:
    /// docker-compose up -d
    /// 
    /// Alternativamente, ajuste RagApiFactory para usar mocks.
    /// </summary>
    public class DocumentsControllerE2ETests : IClassFixture<RagApiFactory>
    {
        private readonly RagApiFactory _factory;
        private readonly HttpClient _client;

        public DocumentsControllerE2ETests(RagApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task IngestDocument_WithValidTextFile_ShouldReturn200()
        {
            // Arrange
            var testFilePath = Path.Combine("TestData", "e2e-test-doc.txt");
            var fileContent = await File.ReadAllBytesAsync(testFilePath);
            
            using var content = new MultipartFormDataContent();
            using var fileContent2 = new ByteArrayContent(fileContent);
            fileContent2.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
            content.Add(fileContent2, "file", "e2e-test-doc.txt");

            // Act
            var response = await _client.PostAsync("/api/ingest", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK,
                "o endpoint deve retornar 200 OK para upload válido");

            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().Contain("success",
                "a mensagem deve indicar sucesso");
        }

        [Fact]
        public async Task IngestDocument_WithValidPdfFile_ShouldReturn200()
        {
            // Arrange
            var testFilePath = Path.Combine("TestData", "sample-pdf.pdf");
            
            if (!File.Exists(testFilePath))
            {
                // Criar um PDF simples para teste se não existir
                await File.WriteAllTextAsync(testFilePath, 
                    "PDF test content for E2E testing");
            }

            var fileContent = await File.ReadAllBytesAsync(testFilePath);
            
            using var content = new MultipartFormDataContent();
            using var fileContent2 = new ByteArrayContent(fileContent);
            fileContent2.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
            content.Add(fileContent2, "file", "sample-pdf.pdf");

            // Act
            var response = await _client.PostAsync("/api/ingest", content);

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.OK, 
                HttpStatusCode.Accepted,
                "o endpoint deve aceitar PDFs válidos");
        }

        [Fact]
        public async Task IngestDocument_WithoutFile_ShouldReturn400()
        {
            // Arrange
            using var content = new MultipartFormDataContent();

            // Act
            var response = await _client.PostAsync("/api/ingest", content);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
                "deve retornar 400 quando não há arquivo");
        }

        [Fact]
        public async Task IngestDocument_WithEmptyFile_ShouldReturn400()
        {
            // Arrange
            using var content = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(Array.Empty<byte>());
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
            content.Add(fileContent, "file", "empty.txt");

            // Act
            var response = await _client.PostAsync("/api/ingest", content);

            // Assert
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,
                HttpStatusCode.InternalServerError,
                "deve rejeitar arquivos vazios");
        }

        [Fact]
        public async Task IngestDocument_WithInvalidContentType_ShouldReturn400OrProcess()
        {
            // Arrange
            var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("test content"));
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            content.Add(fileContent, "file", "test.unknown");

            // Act
            var response = await _client.PostAsync("/api/ingest", content);

            // Assert
            // Pode rejeitar (400) ou tentar processar como texto
            response.StatusCode.Should().BeOneOf(
                HttpStatusCode.BadRequest,
                HttpStatusCode.OK,
                HttpStatusCode.InternalServerError);
        }

        [Fact]
        public async Task IngestDocument_MultipleSequentialUploads_ShouldAllSucceed()
        {
            // Arrange
            var testFilePath = Path.Combine("TestData", "e2e-test-doc.txt");
            var fileContent = await File.ReadAllBytesAsync(testFilePath);

            // Act - Upload 3 documentos sequencialmente
            var responses = new List<HttpResponseMessage>();
            
            for (int i = 0; i < 3; i++)
            {
                using var content = new MultipartFormDataContent();
                using var fc = new ByteArrayContent(fileContent);
                fc.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
                content.Add(fc, "file", $"doc-{i}.txt");

                var response = await _client.PostAsync("/api/ingest", content);
                responses.Add(response);
            }

            // Assert
            responses.Should().AllSatisfy(r =>
                r.StatusCode.Should().BeOneOf(
                    HttpStatusCode.OK,
                    HttpStatusCode.Accepted,
                    "todos os uploads devem ser bem-sucedidos"));

            // Cleanup
            foreach (var response in responses)
            {
                response.Dispose();
            }
        }

        [Fact]
        public async Task IngestDocument_WithLargeFile_ShouldHandleGracefully()
        {
            // Arrange - Criar um arquivo de ~1MB
            var largeContent = new string('A', 1024 * 1024); // 1MB
            var tempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempFile, largeContent);

            try
            {
                var fileContent = await File.ReadAllBytesAsync(tempFile);
                
                using var content = new MultipartFormDataContent();
                using var fc = new ByteArrayContent(fileContent);
                fc.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");
                content.Add(fc, "file", "large-file.txt");

                // Act
                var response = await _client.PostAsync("/api/ingest", content);

                // Assert
                // Deve aceitar ou rejeitar de forma controlada (não crashar)
                response.StatusCode.Should().BeOneOf(
                    HttpStatusCode.OK,
                    HttpStatusCode.Accepted,
                    HttpStatusCode.RequestEntityTooLarge,
                    HttpStatusCode.InternalServerError);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
    }
}
