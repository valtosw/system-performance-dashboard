using Microsoft.AspNetCore.Mvc;
using Server.Models;
using Server.Services;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class PerformanceController : ControllerBase
    {
        [HttpGet("metrics")]
        public ActionResult<PerformanceMetrics> GetMetrics()
        {
            return Ok(PerformanceMonitoringService.LatestMetrics);
        }
    }
}
