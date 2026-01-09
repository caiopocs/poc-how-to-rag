using Microsoft.KernelMemory;

namespace RagApi.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IKernelMemory _memory;

        public DocumentService(IKernelMemory memory)
        {
            _memory = memory;
        }

        public async Task<bool> IngestDocumentAsync(Stream documentStream, string fileName)
        {
            if (documentStream == null || string.IsNullOrEmpty(fileName))
            {
                return false;
            }

            try
            {
                // Import the document into memory
                await _memory.ImportDocumentAsync(documentStream, fileName);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}