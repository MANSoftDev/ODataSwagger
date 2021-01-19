<h1>Swagger documentation with OData</h1>
<h2>1/18/2021</h2>
<ul class="download">
    <li>
        <a href="https://github.com/MANSoftDev/ODataSwagger">https://github.com/MANSoftDev/ODataSwagger</a>
    </li>
</ul>
<p>OData and Swagger are two well-known technologies that separately can provide enhanced 
    capabilities and documentation to your project and are easily implemented with a few lines of code. 
    Getting them to work together, however, takes a little more setup and configuration. 
    I will not be covering the depths of Swagger or OData here; there are a plethora existing articles covering these.
    Rather, this article will look at how to get them to work together.
</p>
<h2>What is Swagger</h2>
<p>Swagger, also known as Open API, is a standard for documenting REST APIs. It doesn't implement documentation but rather defines how the documentation should appear.
    Implementation is via tools, such as Swashbuckle, which is integrated into ASP.NET projects. When creating a new project it is easy to implement by clicking the checkbox.
 </p>
 <img src="https://mansoftdevweb.blob.core.windows.net/images/odataswagger/Image1.png" height="75%">
 <p>This code will be added to the <span class='keyword'>Startup</span> class in the <span class='keyword'>ConfigureServices</span> and <span class='keyword'>Configure</span> 
    methods respectively.</p>
 <pre class="code">
    services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "SwaggerOdata", Version = "v1" });
    });
</pre>
<pre class="code">   
    if (env.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "SwaggerOdata v1"));
    }   
 </pre>
 <img src="https://mansoftdevweb.blob.core.windows.net/images/odataswagger/Image2.png" width="75%">
<h2>What is OData?</h2>
<p>OData, or Open Data Protocol, is a set of best practices for building and consuming REST APIs. Again, I won't be going into depth on OData, but briefly, lets say we have a model such as this</p>
<pre class="code">
    public class TodoItem
    {
        public int ID { get; set; }
        public string Name{ get; set; }
        public bool IsComplete { get; set; } 
        public DateTime Created { get; set; }
        public DateTime? Completed { get; set; }
    }    
</pre>
<p>and a method on the Controller as such</p>
<pre class="code">
    [HttpGet]
    public IEnumerable<TodoItem> Get()   
</pre>
<p>which you would expect to return a collection of <span class="keyword">TodoItems</span> and all the properties for each. What if you were only interested in Name and Created?
The first inclination may be to create another model with only those two properties and return it instead of <span class="keyword">TodoItem</span>. This would be fine until someone wanted only Name or wanted Completed added. Creating multiple models would quickly become unmaintainable.</p>
<p>OData allows you to shape the data by using a querystring parameters, such as,</p>
<ul>
    <li>
        <span class="keyword">http://localhost/api/todo?$select=Name</span>
    </li>
    <li>
        <span class="keyword">http://localhost/api/todo?$select=Name,Created</span>
    </li>
</ul>
<p>In the case of the first query only the Name parameter would be returned in the resultset. Likewise with second, only Name and Created would be returned. This allows for much better support for callers based on their needs and easier to implement, (Actually, there is nothing to implement), then multiple models and endpoints.</p>
<h2>Playing well together</h2>
<p>For this demo I'll start with a default ASP.NET Core Web API project targeted to .NET 5.0.</p>
<p>To get Swagger, or technically Swashbuckle, to recognize OData and play nice together and generate the appropriate documentation we'll need to add additional Nuget packages and create a couple of helper methods.</p>
<p>To start with, add these Nuget packages to the project.</p>
<ul>
    <li>
        <a href="https://www.nuget.org/packages/Microsoft.OData.Core/">Microsoft.OData.Core</a>
    </li>
    <li>
        <a href="https://www.nuget.org/packages/Microsoft.AspNetCore.OData">Microsoft.AspNetCore.OData</a>
    </li>
    <li>
        <a href="https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.ApiExplorer">Microsoft.AspNetCore.MVC.ApiExplorer</a>
    </li>
    <li>
        <a href="https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Versioning.ApiExplorer">Microsoft.AspNetCore.MVC.Versioning.ApiExplorer</a>
    </li>
    <li>
        <a href="https://www.nuget.org/packages/Microsoft.AspNetCore.OData.Versioning/">Microsoft.AspNetCore.OData.Versioning</a>
    </li>
    <li>
        <a href="https://www.nuget.org/packages/Microsoft.AspNetCore.OData.Versioning.ApiExplorer/">Microsoft.AspNetCore.OData.Versioning.ApiExplorer</a>
    </li>
</ul>
<p>As the name implies on the last packages, these add support for versioning. Though technically not required for this scenario, versioning your API is a good practice.</p>
<p>Update <span class="keyword">Startup.cs</span> and replace <span class="keyword">ConfigureServices</span> with this code</p>
<pre class="code">
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
        services.AddTransient&lt;IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSwaggerGen(
            options =>
            {
                // add a custom operation filter which sets default values
                options.OperationFilter&lt;SwaggerDefaultValues>();

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
</pre>
<p>and replace <span class="keyword">Configure</span> with this code</p>
<pre class="code">
    public void Configure(IApplicationBuilder app, 
    IWebHostEnvironment env, 
    VersionedODataModelBuilder modelBuilder, 
    IApiVersionDescriptionProvider provider)
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
</pre>
<p>Again, not going to delve into the specifics of these lines since this article is focused on just getting OData and Swagger working together, not about the nuances of API versioning
    and Swagger generation.
</p>
<p>Now we'll add the classes <span class="keyword">SwaggerDefaultValues</span> and <span class="keyword">ConfigureSwaggerOptions</span></p>
<pre class="code">
    /// <summary>
    /// Represents the Swagger/Swashbuckle operation filter used to document the implicit API version parameter.
    /// </summary>
    /// <remarks>This <see cref="IOperationFilter"/> is only required due to bugs in the <see cref="SwaggerGenerator"/>.
    /// Once they are fixed and published, this class can be removed.</remarks>
    public class SwaggerDefaultValues : IOperationFilter
    {
        /// <summary>
        /// Applies the filter to the specified operation using the given context.
        /// </summary>
        /// <param name="operation">The operation to apply the filter to.</param>
        /// <param name="context">The current operation filter context.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var apiDescription = context.ApiDescription;

            operation.Deprecated |= apiDescription.IsDeprecated();

            if (operation.Parameters == null)
            {
                return;
            }

            foreach (var parameter in operation.Parameters)
            {
                var description = apiDescription.ParameterDescriptions.First(p => p.Name == parameter.Name);

                if (parameter.Description == null)
                {
                    parameter.Description = description.ModelMetadata?.Description;
                }

                if (parameter.Schema.Default == null && description.DefaultValue != null)
                {
                    parameter.Schema.Default = new OpenApiString(description.DefaultValue.ToString());
                }

                parameter.Required |= description.IsRequired;
            }
        }
    }    
</pre><p></p>
<pre class="code">
    /// <summary>
    /// Configures the Swagger generation options.
    /// </summary>
    /// <remarks>This allows API versioning to define a Swagger document per API version after the
    /// <see cref="IApiVersionDescriptionProvider"/> service has been resolved from the service container.</remarks>
    public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly IApiVersionDescriptionProvider _provider;
        private static IConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigureSwaggerOptions"/> class.
        /// </summary>
        /// <param name="provider">The <see cref="IApiVersionDescriptionProvider">provider</see> used to generate Swagger documents.</param>
        public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider, IConfiguration configuration)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <inheritdoc />
        public void Configure(SwaggerGenOptions options)
        {
            // add a swagger document for each discovered API version
            // note: you might choose to skip or document deprecated API versions differently
            foreach (var description in _provider.ApiVersionDescriptions)
            {
                options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
            }
        }

        static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
        {
            var info = new OpenApiInfo();

            // Define values in appConfig for extensibility
            _configuration.GetSection(nameof(OpenApiInfo)).Bind(info);

            info.License = new OpenApiLicense() { Name = "MIT", Url = new Uri("https://opensource.org/licenses/MIT") };

            if (description.IsDeprecated)
            {
                info.Description += " This API version has been deprecated.";
            }

            return info;
        }
    }   
</pre>
<p>We'll also need to generate documentation as the Swagger is built from it.</p>
<img src="https://mansoftdevweb.blob.core.windows.net/images/odataswagger/Image3.png" width="75%">
<p>Now, we'll switch to the <span class="keyword">WeatherForecastController</span> and add the necessary attributes to it and any methods necessary.</p>
<pre class="code">
    [ApiVersion("1.0")]
    [ODataRoutePrefix("WeatherForecast")]
    public class WeatherForecastController : ODataController
</pre>
<p>Two critical pieces here are that <span class="keyword">ODataRoutePrefix</span> must match the name of the controller, WeatherForecast, in this case. Also, it must derive from <span class="keyword">ODataController</span>.</p>
<p>Any method that supports OData will need these attributes applied.</p>
<pre class="code">
    [ODataRoute]
    [EnableQuery]   
</pre>
<p>The first is, of course, setting the route for OData and the latter is what enables a caller to use OData queries. This attribute has a number of options that allow you to 
    control at the method level what queries may be supported, overrriding the global settings added earlier.
</p>
<p>With all of this in place you should now be able to see the Swagger page with the availble methods and OData support.</p>
<img src="https://mansoftdevweb.blob.core.windows.net/images/odataswagger/Image4.png" width="75%">
<h2>Conclusion</h2>
<p>This article was not written with the intention to go into the depths of OData or Swagger. However, I have hopefully given an example of how to enable them in an ASP.NET Core API project 
    and correct any gotchas that may come up.</p>
<h2>References</h2>
<ul>
    <li>
        <a href="https://github.com/microsoft/aspnet-api-versioning/wiki/API-Documentation#aspnet-core-with-odata">https://github.com/microsoft/aspnet-api-versioning/wiki/API-Documentation#aspnet-core-with-odata</a>
    </li>
    <li>
        <a href="https://docs.microsoft.com/en-us/odata/overview">https://docs.microsoft.com/en-us/odata/overview</a>
    </li>
    <li>
        <a href="https://swagger.io/">https://swagger.io/</a>
    </li>
</ul>
