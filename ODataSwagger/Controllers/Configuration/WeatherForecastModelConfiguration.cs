using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNetCore.Mvc;

namespace ODataSwagger.Controllers.Configuration
{
    /// <summary>
    /// Represents the model configuration for orders.
    /// </summary>
    public class WeatherForecastModelConfiguration : IModelConfiguration
    {
        /// <inheritdoc />
        public void Apply(ODataModelBuilder builder, ApiVersion apiVersion, string routePrefix)
        {
            var order = builder.EntitySet<WeatherForecast>("WeatherForecast").EntityType.HasKey(o => o.Id);
        }
    }
}
