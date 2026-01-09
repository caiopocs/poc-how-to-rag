using System;

namespace RagApi.Configuration
{
    public class OllamaOptions
    {
        public string ServiceUrl { get; set; }
        public string ApiKey { get; set; }
        public int Timeout { get; set; }
    }
}