using Testcontainers.Qdrant;
using Microsoft.Extensions.Logging;

namespace RagApi.IntegrationTests.Fixtures
{
    /// <summary>
    /// Fixture para gerenciar o container do Qdrant durante os testes de integração.
    /// O Qdrant é o banco de dados vetorial que armazena os embeddings dos documentos.
    /// 
    /// Utilizamos TestContainers para garantir que cada execução de teste tenha
    /// um banco de dados limpo e isolado, evitando interferências entre testes.
    /// </summary>
    public class QdrantFixture : IAsyncLifetime
    {
        private QdrantContainer? _qdrantContainer;
        private readonly ILogger<QdrantFixture> _logger;

        public string QdrantUrl => _qdrantContainer?.GetConnectionString() ?? string.Empty;
        public const int QdrantPort = 6333;

        public QdrantFixture()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            _logger = loggerFactory.CreateLogger<QdrantFixture>();
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("🚀 Iniciando container do Qdrant...");

            // O TestContainers.Qdrant já configura automaticamente:
            // - Porta exposta (6333)
            // - Health check
            // - Wait strategy
            _qdrantContainer = new QdrantBuilder()
                .WithImage("qdrant/qdrant:latest")
                .WithCleanUp(true)
                .Build();

            await _qdrantContainer.StartAsync();
            _logger.LogInformation($"✅ Qdrant iniciado em: {QdrantUrl}");
        }

        public async Task DisposeAsync()
        {
            if (_qdrantContainer != null)
            {
                _logger.LogInformation("🛑 Parando container do Qdrant...");
                await _qdrantContainer.StopAsync();
                await _qdrantContainer.DisposeAsync();
                _logger.LogInformation("✅ Container do Qdrant parado com sucesso!");
            }
        }
    }
}
