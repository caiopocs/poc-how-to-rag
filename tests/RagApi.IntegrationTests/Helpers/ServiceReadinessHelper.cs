using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace RagApi.IntegrationTests.Helpers
{
    /// <summary>
    /// Helper para aguardar que serviços estejam prontos.
    /// Útil para garantir que containers Docker estão completamente inicializados.
    /// </summary>
    public static class ServiceReadinessHelper
    {
        private static readonly ILogger _logger;

        static ServiceReadinessHelper()
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
            _logger = loggerFactory.CreateLogger("ServiceReadinessHelper");
        }

        /// <summary>
        /// Aguarda até que um endpoint HTTP esteja respondendo.
        /// </summary>
        public static async Task<bool> WaitForHttpEndpointAsync(
            string url,
            TimeSpan timeout,
            TimeSpan checkInterval)
        {
            _logger.LogInformation($"Aguardando endpoint {url} ficar disponível...");

            var stopwatch = Stopwatch.StartNew();
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

            while (stopwatch.Elapsed < timeout)
            {
                try
                {
                    var response = await httpClient.GetAsync(url);
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation($"✅ Endpoint {url} está disponível!");
                        return true;
                    }
                }
                catch
                {
                    // Ignorar erros e tentar novamente
                }

                await Task.Delay(checkInterval);
            }

            _logger.LogWarning($"⚠️ Timeout aguardando {url}");
            return false;
        }

        /// <summary>
        /// Aguarda múltiplos endpoints ficarem disponíveis.
        /// </summary>
        public static async Task<bool> WaitForAllEndpointsAsync(
            IEnumerable<string> urls,
            TimeSpan timeout)
        {
            var checkInterval = TimeSpan.FromSeconds(2);
            var tasks = urls.Select(url => 
                WaitForHttpEndpointAsync(url, timeout, checkInterval));

            var results = await Task.WhenAll(tasks);
            return results.All(r => r);
        }
    }
}
