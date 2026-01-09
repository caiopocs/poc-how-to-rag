using System.ComponentModel.DataAnnotations;

namespace RagApi.Configuration
{
    public class QdrantOptions
    {
        [Required]
        public string Host { get; set; }

        [Required]
        public int Port { get; set; }

        public string ApiKey { get; set; }
    }
}