using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using RagApi.IntegrationTests.Fixtures;

namespace RagApi.IntegrationTests.Fixtures
{
    /// <summary>
    /// Fixture que combina Qdrant + Ollama e configura o Kernel Memory completo.
    /// 
    /// Esta fixture representa o ambiente REAL de RAG:
    /// - Qdrant para armazenar vetores
    /// - Ollama para embeddings e geração de texto
    /// - Kernel Memory orquestrando o pipeline completo
    /// 
    /// Use esta fixture quando quiser testar o fluxo completo de ingestão e recuperação.
    /// </summary>
    public class RagSystemFixture : IAsyncLifetime
    {
        private readonly QdrantFixture _qdrantFixture;
        private readonly OllamaFixture _ollamaFixture;
        
        public IKernelMemory Memory { get; private set; } = null!;
        public string QdrantUrl => _qdrantFixture.QdrantUrl;
        public string OllamaUrl => _ollamaFixture.OllamaUrl;

        public RagSystemFixture()
        {
            _qdrantFixture = new QdrantFixture();
            _ollamaFixture = new OllamaFixture();
        }

        public async Task InitializeAsync()
        {
            // Inicializar os containers em paralelo para economizar tempo
            await Task.WhenAll(
                _qdrantFixture.InitializeAsync(),
                _ollamaFixture.InitializeAsync()
            );

            // Configurar o Kernel Memory com as dependências reais
            Memory = BuildKernelMemory();
        }

        private IKernelMemory BuildKernelMemory()
        {
            var memoryBuilder = new KernelMemoryBuilder()
                .WithCustomTextGenerator(new OllamaTextGenerator(
                    endpoint: OllamaUrl,
                    model: "llama3"))
                .WithCustomEmbeddingGenerator(new OllamaTextEmbeddingGenerator(
                    endpoint: OllamaUrl,
                    model: "nomic-embed-text"))
                .WithQdrantMemoryDb(QdrantUrl);

            // Configurar logging para diagnóstico durante os testes
            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });

            return memoryBuilder.Build<MemoryServerless>();
        }

        public async Task DisposeAsync()
        {
            // Limpar os recursos em paralelo
            await Task.WhenAll(
                _qdrantFixture.DisposeAsync().AsTask(),
                _ollamaFixture.DisposeAsync().AsTask()
            );
        }
    }
}
