using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;

namespace RagApi.E2ETests.Fixtures
{
    /// <summary>
    /// Factory personalizada para testes E2E da API.
    /// 
    /// Esta classe cria uma instância da aplicação Web API em memória,
    /// permitindo testar os endpoints HTTP de forma isolada.
    /// 
    /// IMPORTANTE: Para testes E2E reais, você precisa ter os serviços
    /// Qdrant e Ollama rodando externamente (via docker-compose).
    /// 
    /// Alternativamente, pode configurar mocks para testes mais rápidos.
    /// </summary>
    public class RagApiFactory : WebApplicationFactory<Program>
    {
        public string QdrantUrl { get; set; } = "http://localhost:6333";
        public string OllamaUrl { get; set; } = "http://localhost:11434";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remover o registro existente do KernelMemory (se houver)
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(IKernelMemory));
                
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                // Registrar KernelMemory com URLs de teste
                services.AddSingleton<IKernelMemory>(sp =>
                {
                    var memoryBuilder = new KernelMemoryBuilder()
                        .WithCustomTextGenerator(new OllamaTextGenerator(
                            endpoint: OllamaUrl,
                            model: "llama3"))
                        .WithCustomEmbeddingGenerator(new OllamaTextEmbeddingGenerator(
                            endpoint: OllamaUrl,
                            model: "nomic-embed-text"))
                        .WithQdrantMemoryDb(QdrantUrl);

                    return memoryBuilder.Build<MemoryServerless>();
                });

                // Configurar logging para diagnóstico
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });
            });

            builder.UseEnvironment("Testing");
        }
    }
}
