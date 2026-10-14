using Microsoft.AspNetCore.Mvc;
using RagApp.Application;
using System.Threading;
using System.Threading.Tasks;

namespace RagApp.WebApi.Controllers
{
    [ApiController]
    [Route("api/documents")]
    public class DocumentsController : ControllerBase
    {
        private readonly IRagService _ragService;

        public DocumentsController(IRagService ragService)
        {
            _ragService = ragService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDocuments(CancellationToken cancellationToken)
        {
            var documents = await _ragService.GetDocumentsAsync(cancellationToken);
            return Ok(documents);
        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> DeleteDocument(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                await _ragService.DeleteDocumentAsync(id, cancellationToken);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Document '{id}' was not found.");
            }

            return Ok(new { message = "Document and its chunks were deleted." });
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetDocument(Guid id, CancellationToken cancellationToken)
        {
            var document = await _ragService.GetDocumentAsync(id, cancellationToken);
            if (document is null)
            {
                return NotFound($"Document '{id}' was not found.");
            }

            return Ok(document);
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument([FromBody] DocumentUploadDto dto, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Content))
            {
                return BadRequest("Title and content are required.");
            }

            var id = await _ragService.StartIngestionAsync(dto.Title, dto.Content, cancellationToken);
            return Accepted(new { id, status = "processing", message = "Document queued for ingestion." });
        }
    }

    public record DocumentUploadDto(string Title, string Content);
}
