using System;
using System.IO;
using Microsoft.AspNetCore.Mvc;

namespace auth_dotnet.Controllers
{
    [ApiController]
    [Route("logs")]
    public class LogsController : ControllerBase
    {
        [HttpGet("GetContent")]
        [Route("GetContent")]
        public IActionResult Get(string id)
        {
            try
            {
                // This method is deliberately vulnerable to path traversal, but we want to be very restrictive
                // about what files can be read. We only allow reading logs that end with "SuccessfulEvents".
                // The Jira ticket associated with this vulnerability is DEMOENG-806
                if (id.EndsWith("SuccessfulEvents", StringComparison.OrdinalIgnoreCase))
                {
                    var logContent = System.IO.File.ReadAllText($"{id}.log");
                    return Ok(logContent);
                }
                else
                {
                    return BadRequest("Invalid log name");
                }
            }
            catch (FileNotFoundException)
            {
                return BadRequest("Log was not found");
            }
        }
    }
}
