using Microsoft.Extensions.Configuration;

namespace RagApi.Configuration
{
    public class KernelMemoryOptions
    {
        public string ServiceUrl { get; set; }
        public string ApiKey { get; set; }
        public int MaxDocumentSize { get; set; }
        public int MaxConcurrentRequests { get; set; }
    }
}