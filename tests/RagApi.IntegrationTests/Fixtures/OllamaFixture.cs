using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Logging;

namespace RagApi.IntegrationTests.Fixtures
{
    /// <summary>
    /// Fixture para gerenciar o container do Ollama durante os testes de integração.
    /// O Ollama é essencial para geração de embeddings e respostas LLM.
    /// 
    /// IMPORTANTE: Este container pode levar alguns minutos para baixar os modelos na primeira execução.
    /// Modelos utilizados:
    /// - nomic-embed-text: Para geração de embeddings (vetores)
    /// - llama3: Para geração de respostas (LLM)
    /// </summary>
    public class OllamaFixture : IAsyncLifetime
    {
        private IContainer? _ollamaContainer;
        private readonly ILogger<OllamaFixture> _logger;

        public const int OllamaPort = 11434;
        public string OllamaUrl => $"http://localhost:{OllamaPort}";

        public OllamaFixture()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            _logger = loggerFactory.CreateLogger<OllamaFixture>();
        }

        public async Task InitializeAsync()
        {
            _logger.LogInformation("🚀 Iniciando container do Ollama...");

            // Configuração do container Ollama
            // NOTA: Em ambiente de teste, rodamos sem GPU para compatibilidade
            // Em produção, use a configuração com GPU do docker-compose.yml
            _ollamaContainer = new ContainerBuilder()
                .WithImage("ollama/ollama:latest")
                .WithPortBinding(OllamaPort, 11434)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(11434))
                .WithCleanUp(true)
                .Build();

            await _ollamaContainer.StartAsync();
            _logger.LogInformation("✅ Container do Ollama iniciado com sucesso!");

            // Aguardar o serviço estar completamente disponível
            await Task.Delay(5000);

            // Baixar os modelos necessários (pode levar alguns minutos na primeira vez)
            await PullModelAsync("nomic-embed-text");
            await PullModelAsync("llama3");
        }

        private async Task PullModelAsync(string modelName)
        {
            try
            {
                _logger.LogInformation($"📥 Baixando modelo '{modelName}'... (isso pode levar alguns minutos)");

                var (stdout, stderr, exitCode) = await _ollamaContainer!.ExecAsync(
                    new[] { "ollama", "pull", modelName });

                if (exitCode == 0)
                {
                    _logger.LogInformation($"✅ Modelo '{modelName}' baixado com sucesso!");
                }
                else
                {
                    _logger.LogWarning($"⚠️ Falha ao baixar modelo '{modelName}': {stderr}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erro ao baixar modelo '{modelName}'");
                throw;
            }
        }

        public async Task DisposeAsync()
        {
            if (_ollamaContainer != null)
            {
                _logger.LogInformation("🛑 Parando container do Ollama...");
                await _ollamaContainer.StopAsync();
                await _ollamaContainer.DisposeAsync();
                _logger.LogInformation("✅ Container do Ollama parado com sucesso!");
            }
        }
    }
}
