using log4net;
using log4net.Config;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Squash.DataAccess;
using Squash.Identity;
using Squash.SqlServer;

namespace Squash.Web
{
    public class Program
    {
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
                Log.Info($"Starting Squash.Web in {environment} environment!");

                var builder = WebApplication.CreateBuilder(args);

                bool isContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
                string additionalConfiguration = isContainer ? "Docker" : Environment.MachineName;

                builder.Configuration.AddJsonFile($"appsettings.{environment}.{additionalConfiguration}.json", optional: true, reloadOnChange: true);

                string? connectionString = builder.Configuration.GetConnectionString("DataContext");
                ArgumentException.ThrowIfNullOrEmpty(connectionString);

                builder.Services.AddControllersWithViews();
                builder.Services.AddRazorPages();

                builder.Services.SetupIdentityCore<IdentityUser, IdentityRole>(builder.Configuration, connectionString)
                               .SetupWebAuthentication();

                builder.Services.Configure<RouteOptions>(options => options.LowercaseUrls = true);
                ConfigureMSSql(builder, connectionString);

                if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Test"))
                {
                    builder.Services.AddDatabaseDeveloperPageExceptionFilter();
                }

                builder.Logging.AddLog4Net(new Log4NetProviderOptions { ExternalConfigurationSetup = true });
                builder.Services.AddScoped<IDataContext, DataContext>();

                builder.WebHost.UseStaticWebAssets();

                var app = builder.Build();
                ServiceProvider = app.Services;

                if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Test"))
                {
                    app.UseDeveloperExceptionPage();
                }
                else
                {
                    app.UseExceptionHandler("/Home/Error");
                    app.UseHsts();
                }

                app.UseHttpsRedirection();
                app.UseStaticFiles();
                app.UseRouting();

                app.UseAuthentication();
                app.UseAuthorization();

                app.MapControllers();

                app.MapControllerRoute(
                    name: "match-pin",
                    pattern: "m",
                    defaults: new { area = "Kiosk", controller = "MatchPin", action = "Index" });

                app.MapControllerRoute(
                    name: "areas",
                    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

                app.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");

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

        private static void ConfigureMSSql(WebApplicationBuilder builder, string connectionString)
        {
            builder.Services.AddDbContext<DataContext>(options =>
                options.UseSqlServer(connectionString, x =>
                {
                    x.MigrationsAssembly(typeof(DataContext).Assembly);
                    x.UseCompatibilityLevel(CompatibilityLevel2008RC2);
                }));
        }
    }
}
