using FluentAssertions;
using Microsoft.KernelMemory;
using RagApi.IntegrationTests.Fixtures;
using RagApi.Services;

namespace RagApi.IntegrationTests.Services
{
    /// <summary>
    /// Testes de integração para o ChatService.
    /// 
    /// Estes testes validam o fluxo COMPLETO de recuperação e geração:
    /// 1. Buscar chunks relevantes no Qdrant baseado na pergunta
    /// 2. Ollama gera embeddings da pergunta para comparação semântica
    /// 3. Kernel Memory recupera os top-K chunks mais similares
    /// 4. Ollama (LLM) gera a resposta usando o contexto recuperado
    /// 5. Retorna resposta + citações das fontes
    /// 
    /// Estes testes dependem de documentos previamente ingeridos.
    /// </summary>
    [Collection("RagSystem")]
    public class ChatServiceIntegrationTests
    {
        private readonly RagSystemFixture _fixture;
        private readonly ChatService _chatService;
        private readonly DocumentService _documentService;

        public ChatServiceIntegrationTests(RagSystemFixture fixture)
        {
            _fixture = fixture;
            _chatService = new ChatService(_fixture.Memory);
            _documentService = new DocumentService(_fixture.Memory);
        }

        [Fact]
        public async Task AskQuestionAsync_WithIngestedDocument_ShouldReturnRelevantAnswer()
        {
            // Arrange - Primeiro ingerir um documento
            var testFilePath = Path.Combine("TestData", "sample-document.txt");
            using var fileStream = File.OpenRead(testFilePath);
            await _documentService.IngestDocumentAsync(fileStream, "rag-info.txt");

            // Aguardar processamento
            await Task.Delay(10000);

            // Act - Fazer uma pergunta sobre o conteúdo
            var response = await _chatService.AskQuestionAsync(
                "What are the key benefits of RAG systems?");

            // Assert
            response.Should().NotBeNull();
            response.Answer.Should().NotBeNullOrEmpty("deve gerar uma resposta");
            response.Answer.Should().ContainAny(
                "hallucination", "accuracy", "knowledge", "citation",
                "a resposta deve mencionar benefícios do RAG");
            
            response.Sources.Should().NotBeEmpty("deve incluir as fontes utilizadas");
            response.Sources.Should().Contain(s => s.Contains("rag-info.txt"),
                "deve citar o documento fonte");
        }

        [Fact]
        public async Task AskQuestionAsync_WithMultipleDocuments_ShouldCombineInformation()
        {
            // Arrange - Ingerir múltiplos documentos
            var doc1 = Path.Combine("TestData", "sample-document.txt");
            var doc2 = Path.Combine("TestData", "sample-pdf.pdf");

            using (var stream1 = File.OpenRead(doc1))
            {
                await _documentService.IngestDocumentAsync(stream1, "text-doc.txt");
            }

            using (var stream2 = File.OpenRead(doc2))
            {
                await _documentService.IngestDocumentAsync(stream2, "pdf-doc.pdf");
            }

            await Task.Delay(15000);

            // Act - Fazer pergunta que pode ser respondida por ambos os documentos
            var response = await _chatService.AskQuestionAsync(
                "What technologies are used in RAG systems?");

            // Assert
            response.Should().NotBeNull();
            response.Answer.Should().NotBeNullOrEmpty();
            
            // Deve mencionar tecnologias de ambos os documentos
            response.Answer.ToLower().Should().ContainAny(
                "qdrant", "ollama", "vector", "embedding",
                "deve mencionar tecnologias do RAG");

            // Pode referenciar múltiplas fontes
            response.Sources.Should().NotBeEmpty();
        }

        [Fact]
        public async Task AskQuestionAsync_WithNullQuestion_ShouldReturnEmptyResponse()
        {
            // Act
            var response = await _chatService.AskQuestionAsync(null!);

            // Assert
            response.Should().NotBeNull();
            response.Answer.Should().BeEmpty();
            response.Sources.Should().BeEmpty();
        }

        [Fact]
        public async Task AskQuestionAsync_WithEmptyQuestion_ShouldReturnEmptyResponse()
        {
            // Act
            var response = await _chatService.AskQuestionAsync(string.Empty);

            // Assert
            response.Should().NotBeNull();
            response.Answer.Should().BeEmpty();
            response.Sources.Should().BeEmpty();
        }

        [Fact]
        public async Task AskQuestionAsync_WithUnrelatedQuestion_ShouldIndicateLackOfContext()
        {
            // Arrange - Ingerir documento sobre RAG
            var testFilePath = Path.Combine("TestData", "sample-document.txt");
            using var fileStream = File.OpenRead(testFilePath);
            await _documentService.IngestDocumentAsync(fileStream, "rag-doc.txt");
            await Task.Delay(10000);

            // Act - Fazer pergunta não relacionada ao conteúdo
            var response = await _chatService.AskQuestionAsync(
                "What is the recipe for chocolate cake?");

            // Assert
            response.Should().NotBeNull();
            response.Answer.Should().NotBeNullOrEmpty();
            
            // A resposta deve indicar falta de contexto ou ser genérica
            // O LLM pode dizer que não encontrou informação relevante
            // Ou as fontes podem estar vazias
            if (response.Sources.Any())
            {
                // Se retornou fontes, devem ter baixa relevância
                response.Answer.ToLower().Should().NotContain("rag",
                    "não deve forçar resposta sobre RAG para pergunta sobre bolo");
            }
        }

        [Fact]
        public async Task AskQuestionAsync_SameQuestionMultipleTimes_ShouldBeConsistent()
        {
            // Arrange
            var testFilePath = Path.Combine("TestData", "sample-document.txt");
            using var fileStream = File.OpenRead(testFilePath);
            await _documentService.IngestDocumentAsync(fileStream, "test-doc.txt");
            await Task.Delay(10000);

            var question = "What is a vector database?";

            // Act - Fazer a mesma pergunta 3 vezes
            var response1 = await _chatService.AskQuestionAsync(question);
            var response2 = await _chatService.AskQuestionAsync(question);
            var response3 = await _chatService.AskQuestionAsync(question);

            // Assert - As respostas devem ser semanticamente similares
            response1.Should().NotBeNull();
            response2.Should().NotBeNull();
            response3.Should().NotBeNull();

            // Todas devem mencionar conceitos relevantes
            var allAnswers = new[] { response1.Answer, response2.Answer, response3.Answer };
            foreach (var answer in allAnswers)
            {
                answer.ToLower().Should().ContainAny("vector", "database", "qdrant", "embedding",
                    "cada resposta deve abordar o conceito de vector database");
            }

            // As fontes devem ser as mesmas (mesmo documento)
            response1.Sources.Should().BeEquivalentTo(response2.Sources);
            response2.Sources.Should().BeEquivalentTo(response3.Sources);
        }

        [Fact]
        public async Task AskQuestionAsync_WithSpecificDetails_ShouldExtractPreciseInformation()
        {
            // Arrange
            var testFilePath = Path.Combine("TestData", "sample-document.txt");
            using var fileStream = File.OpenRead(testFilePath);
            await _documentService.IngestDocumentAsync(fileStream, "details.txt");
            await Task.Delay(10000);

            // Act - Pergunta específica que requer busca precisa
            var response = await _chatService.AskQuestionAsync(
                "How many key benefits of RAG are listed?");

            // Assert
            response.Should().NotBeNull();
            response.Answer.Should().NotBeNullOrEmpty();
            
            // Deve mencionar um número (o documento lista 4 benefícios)
            response.Answer.Should().MatchRegex(@"\b[0-9]+\b",
                "deve incluir um número ao contar benefícios");
        }
    }
}
