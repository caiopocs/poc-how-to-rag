using Microsoft.KernelMemory;

namespace RagApi.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IKernelMemory _memory;
        private readonly ILogger<DocumentService> _logger;

        public DocumentService(IKernelMemory memory, ILogger<DocumentService> logger)
        {
            _memory = memory;
            _logger = logger;
        }

        public async Task<bool> IngestDocumentAsync(Stream documentStream, string fileName)
        {
            if (documentStream == null || string.IsNullOrEmpty(fileName))
            {
                _logger.LogWarning("Invalid document stream or filename");
                return false;
            }

            try
            {
                _logger.LogInformation("Starting document ingestion: {FileName}", fileName);
                
                // Import the document into memory
                var documentId = await _memory.ImportDocumentAsync(documentStream, fileName);
                
                _logger.LogInformation("Document ingested successfully: {FileName}, ID: {DocumentId}", fileName, documentId);
                
                // Wait a moment and check if document is ready
                await Task.Delay(2000);
                var isReady = await _memory.IsDocumentReadyAsync(documentId);
                _logger.LogInformation("Document ready status: {IsReady} for ID: {DocumentId}", isReady, documentId);
                
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ingesting document: {FileName}", fileName);
                return false;
            }
        }
    }
}