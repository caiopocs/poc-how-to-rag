using Microsoft.AspNetCore.Mvc;
using RagApi.Services;

namespace RagApi.Controllers
{
    [ApiController]
    [Route("api")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;

        public DocumentsController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        [HttpPost("ingest")]
        public async Task<IActionResult> IngestDocument(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            using var stream = file.OpenReadStream();
            var result = await _documentService.IngestDocumentAsync(stream, file.FileName);
            
            if (result)
            {
                return Ok(new { message = "Document ingested successfully.", fileName = file.FileName });
            }

            return StatusCode(500, "Error ingesting document.");
        }
    }
}