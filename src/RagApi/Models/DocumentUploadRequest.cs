using System.ComponentModel.DataAnnotations;

namespace RagApi.Models
{
    public class DocumentUploadRequest
    {
        [Required]
        public string FileName { get; set; }

        [Required]
        public byte[] FileContent { get; set; }

        [Required]
        public string FileType { get; set; }
    }
}