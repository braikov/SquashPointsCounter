using Asp.Versioning;
using Asp.Versioning.ApiExplorer;
using log4net;
using log4net.Config;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.OpenApi.Models;
using Squash.DataAccess;
using Squash.Identity;
using System.Text.Json.Serialization;
using SquashDataContext = Squash.SqlServer.DataContext;

namespace Squash.API
{
    public class Program
    {
        public const string AllowSpecificOrigins = "CorsDomainsAllowed";
        public const string AllowEverything = "PermissivePolicy";
        private const int CompatibilityLevel2008RC2 = 100;
        private static readonly ILog Log = LogManager.GetLogger(typeof(Program));

        public static IServiceProvider ServiceProvider { get; set; } = null!;

        public static void Main(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? throw new Exception("Cannot get 'ASPNETCORE_ENVIRONMENT' variable");

            try
            {
                ConfigureLog4net(environment);
                Log.Info($"Starting Squash.API in {environment} environment!");

                var builder = WebApplication.CreateBuilder(args);

                bool isContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
                string additionalConfiguration = isContainer ? "Docker" : Environment.MachineName;

                builder.Configuration.AddJsonFile($"appsettings.{environment}.{additionalConfiguration}.json", optional: true, reloadOnChange: true);

                string? connectionString = builder.Configuration.GetConnectionString("DataContext");
                ArgumentException.ThrowIfNullOrEmpty(connectionString);

                builder.Services
                    .AddApiVersioning(options =>
                    {
                        options.DefaultApiVersion = new ApiVersion(1, 0);
                        options.AssumeDefaultVersionWhenUnspecified = true;
                        options.ReportApiVersions = true;
                        options.ApiVersionReader = new UrlSegmentApiVersionReader();
                    })
                    .AddApiExplorer(options =>
                    {
                        options.GroupNameFormat = "'v'VVV";
                        options.SubstituteApiVersionInUrl = true;
                    });

                builder.Services.AddControllers().AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = null;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

                builder.Services.SetupIdentityCore<IdentityUser, IdentityRole>(builder.Configuration, connectionString)
                                .SetupApiAuthentication(builder.Configuration);

                builder.Services.AddResponseCaching();
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

                ConfigureSwagger(builder);
                ConfigureMSSql(builder, connectionString);

                builder.Logging.AddLog4Net(new Log4NetProviderOptions { ExternalConfigurationSetup = true });
                builder.Services.AddScoped<IDataContext, SquashDataContext>();

                bool hasCors = ConfigureCors(builder);

                var app = builder.Build();
                ServiceProvider = app.Services;

                app.UseExceptionHandler(_ => { });

                bool useSwagger = builder.Configuration.GetSection("EnableSwagger").Get<bool>();
                if (useSwagger)
                {
                    var apiVersions = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

                    app.UseSwagger();
                    app.UseSwaggerUI(ui =>
                    {
                        foreach (var desc in apiVersions.ApiVersionDescriptions)
                        {
                            ui.SwaggerEndpoint($"/swagger/{desc.GroupName}/swagger.json",
                                               desc.GroupName.ToUpperInvariant());
                        }
                    });
                }

                app.UseHttpsRedirection();

                if (hasCors)
                {
                    app.UseCors(AllowSpecificOrigins);
                }

                app.UseResponseCaching();
                app.UseAuthentication();
                app.UseAuthorization();

                app.MapControllers();
                app.MapRazorPages();

                app.Run();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        private static void ConfigureLog4net(string? environment)
        {
            var logRepository = LogManager.GetRepository(System.Reflection.Assembly.GetEntryAssembly()!);
            bool isContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
            string additionalConfiguration = isContainer ? "Docker" : Environment.MachineName;
            var log4NetConfigFile = new FileInfo($"log4net.{environment}.{additionalConfiguration}.config");
            if (!log4NetConfigFile.Exists)
            {
                log4NetConfigFile = new FileInfo($"log4net.{environment}.config");
            }
            XmlConfigurator.Configure(logRepository, log4NetConfigFile);
        }

        private static void ConfigureSwagger(WebApplicationBuilder builder)
        {
            builder.Services.AddSwaggerGen(option =>
            {
                var provider = builder.Services
                                         .BuildServiceProvider()
                                         .GetRequiredService<IApiVersionDescriptionProvider>();

                foreach (var desc in provider.ApiVersionDescriptions)
                {
                    option.SwaggerDoc(
                        desc.GroupName,
                        new OpenApiInfo { Title = $"Squash API {desc.ApiVersion}", Version = desc.ApiVersion.ToString() }
                    );
                }

                option.DocInclusionPredicate((docName, apiDesc) =>
                    apiDesc.GroupName?.Equals(docName, StringComparison.OrdinalIgnoreCase) == true);

                option.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Name = "ApiKey",
                    Type = SecuritySchemeType.ApiKey,
                    Description = "API Key needed to access the endpoints"
                });

                option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                option.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "ApiKey"
                            }
                        },
                        Array.Empty<string>()
                    },
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });
        }

        private static void ConfigureMSSql(WebApplicationBuilder builder, string connectionString)
        {
            builder.Services.AddDbContext<SquashDataContext>(options =>
                options.UseSqlServer(connectionString, x =>
                {
                    x.MigrationsAssembly(typeof(SquashDataContext).Assembly);
                    x.UseCompatibilityLevel(CompatibilityLevel2008RC2);
                }));
        }

        private static bool ConfigureCors(WebApplicationBuilder builder)
        {
            string[]? allowedCorsHosts = builder.Configuration.GetSection("CorsAllowedHosts").Get<string[]>() ?? ["*"];

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: AllowSpecificOrigins,
                    policy =>
                    {
                        policy.WithOrigins(allowedCorsHosts)
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    });

                options.AddPolicy(name: AllowEverything,
                    policy =>
                    {
                        policy.AllowAnyOrigin()
                            .DisallowCredentials()
                            .AllowAnyMethod()
                            .AllowAnyHeader();
                    });
            });

            return true;
        }
    }
}