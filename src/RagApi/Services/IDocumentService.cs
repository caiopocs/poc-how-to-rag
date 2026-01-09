namespace RagApi.Services
{
    public interface IDocumentService
    {
        Task<bool> IngestDocumentAsync(Stream documentStream, string fileName);
    }
}