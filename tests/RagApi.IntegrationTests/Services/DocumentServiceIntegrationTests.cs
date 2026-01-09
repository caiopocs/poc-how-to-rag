using FluentAssertions;
using Microsoft.KernelMemory;
using RagApi.IntegrationTests.Fixtures;
using RagApi.Services;

namespace RagApi.IntegrationTests.Services
{
    /// <summary>
    /// Testes de integração para o DocumentService.
    /// 
    /// Estes testes validam o fluxo COMPLETO de ingestão:
    /// 1. Receber um documento (TXT ou PDF)
    /// 2. Kernel Memory faz o chunking automático
    /// 3. Ollama gera os embeddings para cada chunk
    /// 4. Qdrant armazena os vetores
    /// 
    /// IMPORTANTE: Estes testes são LENTOS (podem levar minutos) porque:
    /// - Inicializam containers Docker (Qdrant + Ollama)
    /// - Baixam modelos de IA na primeira execução
    /// - Processam documentos reais
    /// 
    /// Execute estes testes quando quiser validar a integração completa.
    /// </summary>
    [Collection("RagSystem")]
    public class DocumentServiceIntegrationTests
    {
        private readonly RagSystemFixture _fixture;
        private readonly DocumentService _documentService;

        public DocumentServiceIntegrationTests(RagSystemFixture fixture)
        {
            _fixture = fixture;
            _documentService = new DocumentService(_fixture.Memory);
        }

        [Fact]
        public async Task IngestDocumentAsync_WithValidTextFile_ShouldIngestSuccessfully()
        {
            // Arrange
            var testFilePath = Path.Combine("TestData", "sample-document.txt");
            var fileName = "sample-document.txt";

            using var fileStream = File.OpenRead(testFilePath);

            // Act
            var result = await _documentService.IngestDocumentAsync(fileStream, fileName);

            // Assert
            result.Should().BeTrue("o documento deve ser processado com sucesso");

            // Aguardar o processamento assíncrono do Kernel Memory
            // O Kernel Memory processa em background: chunk -> embed -> save
            await Task.Delay(10000); // 10 segundos

            // Verificar se o documento foi realmente armazenado fazendo uma busca
            var searchResult = await _fixture.Memory.AskAsync(
                "What is RAG?",
                minRelevance: 0.5);

            searchResult.Should().NotBeNull();
            searchResult.Result.Should().NotBeNullOrEmpty("deve haver uma resposta baseada no documento ingerido");
            searchResult.RelevantSources.Should().NotBeEmpty("deve referenciar o documento como fonte");
        }

        [Fact]
        public async Task IngestDocumentAsync_WithValidPdfFile_ShouldIngestSuccessfully()
        {
            // Arrange
            var testFilePath = Path.Combine("TestData", "sample-pdf.pdf");
            var fileName = "sample-pdf.pdf";

            using var fileStream = File.OpenRead(testFilePath);

            // Act
            var result = await _documentService.IngestDocumentAsync(fileStream, fileName);

            // Assert
            result.Should().BeTrue("o PDF deve ser processado com sucesso");

            // Aguardar processamento
            await Task.Delay(10000);

            // Verificar se podemos fazer perguntas sobre o conteúdo do PDF
            var searchResult = await _fixture.Memory.AskAsync(
                "What vector database is mentioned?",
                minRelevance: 0.5);

            searchResult.Should().NotBeNull();
            searchResult.Result.Should().Contain("Qdrant", "deve mencionar o Qdrant do PDF");
        }

        [Fact]
        public async Task IngestDocumentAsync_WithNullStream_ShouldReturnFalse()
        {
            // Arrange
            Stream? nullStream = null;
            var fileName = "test.txt";

            // Act
            var result = await _documentService.IngestDocumentAsync(nullStream!, fileName);

            // Assert
            result.Should().BeFalse("não deve processar um stream nulo");
        }

        [Fact]
        public async Task IngestDocumentAsync_WithEmptyFileName_ShouldReturnFalse()
        {
            // Arrange
            using var stream = new MemoryStream();
            var emptyFileName = string.Empty;

            // Act
            var result = await _documentService.IngestDocumentAsync(stream, emptyFileName);

            // Assert
            result.Should().BeFalse("não deve processar com nome de arquivo vazio");
        }

        [Fact]
        public async Task IngestDocumentAsync_MultipleDocuments_ShouldAllBeSearchable()
        {
            // Arrange - Ingerir múltiplos documentos
            var doc1Path = Path.Combine("TestData", "sample-document.txt");
            var doc2Path = Path.Combine("TestData", "sample-pdf.pdf");

            // Act
            using (var stream1 = File.OpenRead(doc1Path))
            {
                await _documentService.IngestDocumentAsync(stream1, "doc1.txt");
            }

            using (var stream2 = File.OpenRead(doc2Path))
            {
                await _documentService.IngestDocumentAsync(stream2, "doc2.pdf");
            }

            // Aguardar processamento de ambos
            await Task.Delay(15000);

            // Assert - Fazer perguntas que referenciam ambos os documentos
            var result1 = await _fixture.Memory.AskAsync("Tell me about vector databases");
            var result2 = await _fixture.Memory.AskAsync("What is Ollama?");

            result1.Should().NotBeNull();
            result1.RelevantSources.Should().NotBeEmpty();

            result2.Should().NotBeNull();
            result2.RelevantSources.Should().NotBeEmpty();
        }
    }

    /// <summary>
    /// Collection definition para compartilhar a mesma fixture entre múltiplos testes.
    /// Isso evita recriar os containers Docker para cada classe de teste, economizando tempo.
    /// </summary>
    [CollectionDefinition("RagSystem")]
    public class RagSystemCollection : ICollectionFixture<RagSystemFixture>
    {
        // Esta classe é apenas um marcador para xUnit
        // Todos os testes na collection "RagSystem" compartilham a mesma fixture
    }
}
