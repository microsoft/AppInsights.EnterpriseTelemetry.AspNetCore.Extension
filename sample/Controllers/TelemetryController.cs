using System;
using Microsoft.AspNetCore.Mvc;
using AppInsights.EnterpriseTelemetry;
using AppInsights.EnterpriseTelemetry.Context;
using AppInsights.EnterpriseTelemetry.Web.Extension.Filters;
using AppInsights.EnterpriseTelemetry.AspNetCore.Extension.Sample;

namespace Telemetry.Web.Tests.Controllers
{
    [ServiceFilter(typeof(RequestResponseLoggerFilterAttribute))]
    [Route("api/telemetry")]
    public class TelemetryController: ControllerBase
    {
        private readonly ILogger _logger;

        public TelemetryController(ILogger logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Route("/api/probe/ping")]
        public IActionResult Ping()
        {
            return new OkObjectResult("Pong");
        }

        [HttpPost]
        [Route("message")]
        public IActionResult LogMessage([FromQuery] string message)
        {
            var messageContext = new MessageContext(message);
            _logger.LogRequestBody(message);
            _logger.Log(messageContext);
            return Ok("Message Logged");
        }

        [HttpPost]
        [Route("exception")]
        public IActionResult LogException([FromQuery] string message)
        {
            var context = new ExceptionContext(new Exception(message));
            _logger.LogRequestBody(message);
            _logger.Log(context);
            return Ok("Exception Logged");
        }

        [HttpPost]
        [Route("event")]
        public IActionResult LogMessage([FromBody] EventContext context)
        {   
            _logger.Log(context);
            return Ok("Message Logged");
        }

        [HttpPost]
        [Route("metric")]
        public IActionResult LogMessage([FromBody] MetricContext context)
        {
            _logger.Log(context);
            return Ok("Message Logged");
        }

        [HttpPost]
        [Route("error")]
        public IActionResult ThrowException([FromQuery] string errorMessage)
        {
            new UnhandledExceptionGenerator().Generate(errorMessage);
            return new OkObjectResult("Should throw error");
        }
    }
}
