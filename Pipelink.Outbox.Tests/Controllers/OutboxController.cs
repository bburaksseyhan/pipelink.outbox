using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pipelink.Outbox.Data;
using Pipelink.Outbox.Interfaces;
using Pipelink.Outbox.Models;

namespace Pipelink.Outbox.Tests.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OutboxController : ControllerBase
    {
        private readonly IOutboxPipeline<OutboxMessage> _pipeline;
        private readonly OutboxDbContext _context;

        public OutboxController(IOutboxPipeline<OutboxMessage> pipeline, OutboxDbContext context)
        {
            _pipeline = pipeline;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateMessage([FromBody] CreateMessageRequest request)
        {
            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                MessageType = request.MessageType,
                Payload = request.Payload,
                Status = "Pending"
            };

            await _pipeline.ProcessAsync(message);

            return Ok(new { MessageId = message.Id });
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages()
        {
            var messages = await _context.OutboxMessages.ToListAsync();
            return Ok(messages);
        }
    }

    public class CreateMessageRequest
    {
        public required string MessageType { get; set; }
        public required string Payload { get; set; }
    }
} 