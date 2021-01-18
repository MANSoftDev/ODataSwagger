using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using static Microsoft.AspNet.OData.Query.AllowedQueryOptions;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace ODataSwagger.Controllers
{
    /// <summary>
    /// Represents a RESTful service for Weather.
    /// </summary>
    [ApiVersion("1.0")]
    [ODataRoutePrefix("WeatherForecast")]
    public class WeatherForecastController : ODataController
    {
        private static readonly string[] _summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<WeatherForecastController> _logger;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger"><see cref="ILogger"/> implementation for logging</param>
        public WeatherForecastController(ILogger<WeatherForecastController> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Get random weather forecast
        /// </summary>
        /// <returns>Collection of <see cref="WeatherForecast"/></returns>
        [HttpGet]
        [ODataRoute]
        [Produces("application/json")]
        [ProducesResponseType(typeof(WeatherForecast), Status200OK)]
        [EnableQuery]
        public IEnumerable<WeatherForecast> Get()
        {
            var rng = new Random();
            return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = rng.Next(-20, 55),
                Summary = _summaries[rng.Next(_summaries.Length)]
            })
            .ToArray();
        }
    }
}
