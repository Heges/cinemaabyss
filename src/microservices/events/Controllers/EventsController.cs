using events.Models.API;
using events.Services.Kafka;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Channels;

namespace events.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class EventsController : ControllerBase
    {
        private readonly IEventRegistry _registry;
        private readonly ILogger<EventsController> _logger;

        public EventsController(IEventRegistry registry, ILogger<EventsController> logger)
        {
            _registry = registry;
            _logger = logger;
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = true });
        }

        [HttpPost("movie")]
        public Task<IActionResult> CreateMovieEvent([FromBody] MovieRequest request, CancellationToken ct)
        {
            return EnqueueAsync("movie", request, ct);
        }

        [HttpPost("user")]
        public Task<IActionResult> CreateUserEvent([FromBody] UserRequest request, CancellationToken ct)
        {
            return EnqueueAsync("user", request, ct);
        }

        [HttpPost("payment")]
        public Task<IActionResult> CreatePaymentEvent([FromBody] PaymentRequest request, CancellationToken ct)
        {
            return EnqueueAsync("payment", request, ct);
        }

        private async Task<IActionResult> EnqueueAsync(string eventType, object payload, CancellationToken ct)
        {
            try
            {
                var evt = await _registry.RegisterAsync(eventType, payload, ct);

                _logger.LogInformation(
                    "Kafka event accepted. Type={EventType} EventId={EventId}",
                    eventType,
                    evt.Id);

                return StatusCode(
                    StatusCodes.Status201Created,
                    new EventResponse
                    {
                        Status = "success",
                        Queued = true,
                        Event = evt
                    });
            }
            catch (ChannelClosedException ex)
            {
                _logger.LogError(ex, "Kafka producer queue is closed. Type={EventType}", eventType);

                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new { status = "error", message = "Event producer queue is unavailable" });
            }
        }
    }
}
