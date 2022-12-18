using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AzB2CWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PublicController : ControllerBase
    {
        private readonly ILogger<PublicController> _logger;

        public PublicController(ILogger<PublicController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            await Task.FromResult(0);
            return Ok(new { message = "you are a guest!" });
        }
    }
}
