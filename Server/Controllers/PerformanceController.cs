using System.Runtime.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Server.Models;
using Server.Services;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public sealed class PerformanceController : ControllerBase
    {
        [SupportedOSPlatform("windows")]
        [HttpGet("metrics")]
        public ActionResult<PerformanceMetrics> GetMetrics()
        {
            return Ok(PerformanceMonitoringService.LatestMetrics);
        }
    }
}
