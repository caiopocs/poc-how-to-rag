using Microsoft.AspNetCore.Mvc;
using RagApi.Services;

namespace RagApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;

        public DocumentsController(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        /// <summary>
        /// Upload and ingest a document (PDF, TXT, DOCX, etc.) into the RAG system
        /// </summary>
        /// <param name="file">The document file to upload</param>
        /// <returns>Confirmation of successful ingestion</returns>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UploadDocument(IFormFile file)
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