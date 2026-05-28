using Microsoft.AspNetCore.Mvc;
using KyloLabs.DevIAHelper.Console.Interfaces.Services;
using KyloLabs.DevIAHelper.Console.Models.Request;

namespace KyloLabs.DevIAHelper.Console.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DevIAHelperController : ControllerBase
    {
        private readonly IDevIAHelperService _devIAHelperService;

        public DevIAHelperController(IDevIAHelperService devIAHelperService)
        {
            _devIAHelperService = devIAHelperService;
        }

        [HttpPost]
        public async Task<IActionResult> Input([FromBody] InputRequestModel request)
        {
            var result = await _devIAHelperService.Input(request.Input);
            if (result.IsError)
                return StatusCode(500, result);
            return Ok(result);
        }

        [HttpGet("status")]
        public IActionResult GetStatus()
        {
            return Ok(new
            {
                Status = "Running",
                Model = "Qwen-2.5-Coder-7B",
                Memory = GC.GetTotalMemory(false) / 1024 / 1024 + " MB"
            });
        }
    }
}
