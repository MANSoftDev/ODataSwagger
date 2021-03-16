using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Formatter;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.Net.Http.Headers;

using ODataSwagger.Swagger;

using Swashbuckle.AspNetCore.SwaggerGen;

namespace ODataSwagger
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        /// <summary>
        /// Configures services for the application.
        /// </summary>
        /// <param name="services">The collection of services to configure the application with.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddApiVersioning(options =>
            {
                options.ReportApiVersions = true;
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
                // Allow version to be specified in header
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new QueryStringApiVersionReader(),
                    new HeaderApiVersionReader("api-version", "x-ms-version"));
            });
            services.AddOData().EnableApiVersioning();
            // Place after AddOData to avoid issues with formatters not being available
            services.AddMvcCore(options =>
            {
                var formatters =
                    options.OutputFormatters.OfType<ODataOutputFormatter>()
                    .Where(_ => _.SupportedMediaTypes.Count == 0);

                foreach (var formatter in formatters)
                {
                    formatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("application/odata"));
                }
            });

            services.AddODataApiExplorer(
                options =>
                {
                    // add the versioned api explorer, which also adds IApiVersionDescriptionProvider service
                    // note: the specified format code will format the version as "'v'major[.minor][-status]"
                    options.GroupNameFormat = "'v'VVV";

                    // note: this option is only necessary when versioning by url segment. the SubstitutionFormat
                    // can also be used to control the format of the API version in route templates
                    options.SubstituteApiVersionInUrl = true;
                });
            services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
            services.AddSwaggerGen(
                options =>
                {
                    // add a custom operation filter which sets default values
                    options.OperationFilter<SwaggerDefaultValues>();

                    // integrate xml comments
                    options.IncludeXmlComments(XmlCommentsFilePath());
                });

            string XmlCommentsFilePath()
            {
                var basePath = PlatformServices.Default.Application.ApplicationBasePath;
                var fileName = typeof(Startup).GetTypeInfo().Assembly.GetName().Name + ".xml";
                return Path.Combine(basePath, fileName);
            }
        }

        /// <summary>
        /// Configures the application using the provided builder, hosting environment, and logging factory
        /// </summary>
        /// <param name="app">The current application builder</param>
        /// <param name="env">Current environment</param>
        /// <param name="modelBuilder">The <see cref="VersionedODataModelBuilder">model builder</see> used to create OData entity data models (EDMs)</param>
        /// <param name="provider">The API version descriptor provider used to enumerate defined API versions</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, VersionedODataModelBuilder modelBuilder, IApiVersionDescriptionProvider provider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                // Indicate what OData methods to support
                endpoints.Select().Filter().Count().OrderBy();
                endpoints.MapVersionedODataRoute("odata", "api", modelBuilder);
                endpoints.EnableDependencyInjection();
            });

            app.UseSwagger();
            app.UseSwaggerUI(
                options =>
                {
                    // build a swagger endpoint for each discovered API version
                    foreach (var description in provider.ApiVersionDescriptions)
                    {
                        options.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                    }
                });
        }
    }
}
