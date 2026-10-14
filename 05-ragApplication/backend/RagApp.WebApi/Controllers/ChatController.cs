using System;
using Microsoft.AspNetCore.Mvc;
using RagApp.Application;
using System.Threading;
using System.Threading.Tasks;

namespace RagApp.WebApi.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly IRagService _ragService;

        public ChatController(IRagService ragService)
        {
            _ragService = ragService;
        }

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] ChatQueryDto dto, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(dto.Question))
            {
                return BadRequest("Question is required.");
            }

            var answer = await _ragService.AskAsync(dto.Question, dto.DocumentIds, cancellationToken);
            return Ok(new { answer });
        }
    }

    public record ChatQueryDto(string Question, Guid[]? DocumentIds);
}