using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http.Features;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Swagger;
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.Console;
using HetsApi.Authorization;
using HetsApi.Authentication;
using HetsData.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.Hosting;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace HetsApi
{
    /// <summary>
    /// Application startup class
    /// </summary>
    public class Startup
    {
        private readonly IWebHostEnvironment _hostingEnv;

        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            _hostingEnv = env;
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            string connectionString = GetConnectionString();

            // add http context accessor
            services.AddHttpContextAccessor();

            // add database context
            services.AddDbContext<DbAppContext>(options => options.UseNpgsql(connectionString));

            services
                .AddControllers(options =>
                {
                    var policy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
                    options.Filters.Add(new AuthorizeFilter(policy));
                })
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
                    options.SerializerSettings.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat;
                    options.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Utc;
                    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            // setup SiteMinder authentication (core 2.0+)
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = SiteMinderAuthOptions.AuthenticationSchemeName;
                options.DefaultChallengeScheme = SiteMinderAuthOptions.AuthenticationSchemeName;
            }).AddSiteMinderAuth(options =>
            {
            });

            // setup authorization
            services.AddAuthorization();
            services.RegisterPermissionHandler();

            // allow for large files to be uploaded
            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 1073741824; // 1 GB
            });

            // enable Hangfire
            PostgreSqlStorageOptions postgreSqlStorageOptions = new PostgreSqlStorageOptions {
                SchemaName = "public"
            };
            if (connectionString != null)
            {
                PostgreSqlStorage storage = new PostgreSqlStorage(connectionString, postgreSqlStorageOptions);
                services.AddHangfire(config =>
                {
                    config.UseStorage(storage);
                    config.UseConsole();
                });
            }

            // Configure Swagger - only required in the Development Environment
            if (_hostingEnv.IsDevelopment())
            {
                services.AddSwaggerGen(options =>
                {
                    options.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Version = "v1",
                        Title = "HETS REST API",
                        Description = "Hired Equipment Tracking System"
                    });
                });
            }
        }

        /// <summary>
        /// Configure the HTTP request pipeline
        /// </summary>
        /// <param name="app"></param>
        /// <param name="env"></param>
        /// <param name="loggerFactory"></param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILogger<Startup> logger)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            //app.UseMiddleware<ExceptionMiddleware>();

            //TryMigrateDatabase(app, logger);

            //var healthCheckOptions = new HealthCheckOptions
            //{
            //    ResponseWriter = async (c, r) =>
            //    {
            //        c.Response.ContentType = MediaTypeNames.Application.Json;
            //        var result = JsonSerializer.Serialize(
            //           new
            //           {
            //               checks = r.Entries.Select(e =>
            //          new {
            //              description = e.Key,
            //              status = e.Value.Status.ToString(),
            //              tags = e.Value.Tags,
            //              responseTime = e.Value.Duration.TotalMilliseconds
            //          }),
            //               totalResponseTime = r.TotalDuration.TotalMilliseconds
            //           });
            //        await c.Response.WriteAsync(result);
            //    }
            //};

            //app.UseHealthChecks("/healthz", healthCheckOptions);

            app.UseRouting();
            app.UseCors();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint(Configuration.GetSection("Constants:SwaggerApiUrl").Value, "HETS REST API v1");
                options.DocExpansion(DocExpansion.None);
            });

            // enable Hangfire Dashboard
            // disable the back to site link
            DashboardOptions dashboardOptions = new DashboardOptions
            {
                AppPath = null,
                Authorization = new[] { new HangfireAuthorizationFilter() }
            };

            // enable the hangfire dashboard
            app.UseHangfireDashboard(Configuration.GetSection("Constants:HangfireUrl").Value, dashboardOptions);

            BackgroundJobServerOptions jsOptions = new BackgroundJobServerOptions
            {
                WorkerCount = 1                
            };

            app.UseHangfireServer(jsOptions);
        }

        /// <summary>
        /// Retrieve database connection string
        /// </summary>
        /// <returns></returns>
        private string GetConnectionString()
        {
            string connectionString;

            string host = Configuration["DATABASE_SERVICE_NAME"];
            string username = Configuration["POSTGRESQL_USER"];
            string password = Configuration["POSTGRESQL_PASSWORD"];
            string database = Configuration["POSTGRESQL_DATABASE"];

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(database))
            {
                connectionString = Configuration.GetConnectionString("HETS");
            }
            else
            {
                // environment variables override all other settings (OpenShift)
                connectionString = $"Host={host};Username={username};Password={password};Database={database};";
            }

            return connectionString;
        }
    }
}
