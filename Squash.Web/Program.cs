using log4net;
using log4net.Config;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Squash.DataAccess;
using Squash.Identity;
using Squash.Shared.Logging;
using Squash.SqlServer;
using Squash.Web.Localization;
using Squash.Web.Areas.Public.Services.Routing;
using Squash.Web.Areas.Public.Services.Sitemap;
using System.Globalization;

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
                Log4NetTableEnsurer.Ensure(connectionString);

                builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
                builder.Services.AddControllersWithViews()
                    .AddViewLocalization()
                    .AddDataAnnotationsLocalization();
                builder.Services.AddRazorPages()
                    .AddViewLocalization();
                builder.Services.AddHttpContextAccessor();
                builder.Services.AddDistributedMemoryCache();
                builder.Services.AddSession(options =>
                {
                    options.Cookie.Name = ".Squash.Session";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.IsEssential = true;
                    options.IdleTimeout = TimeSpan.FromHours(2);
                });

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
                builder.Services.AddScoped<IPublicUrlBuilder, PublicUrlBuilder>();
                builder.Services.AddScoped<ISitemapEntryStore, DbSitemapEntryStore>();
                builder.Services.AddScoped<ISitemapEntryProvider, DbSitemapEntryProvider>();
                builder.Services.AddScoped<ISitemapEntryProvider, TournamentSitemapEntryProvider>();
                builder.Services.AddScoped<ISitemapBuilder, SitemapBuilder>();

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
                app.UseSession();
                app.UseRouting();

                var supportedCultures = new[]
                {
                    new CultureInfo("bg-BG"),
                    new CultureInfo("en-GB")
                };
                var localizationOptions = new RequestLocalizationOptions
                {
                    DefaultRequestCulture = new RequestCulture("en-GB"),
                    SupportedCultures = supportedCultures,
                    SupportedUICultures = supportedCultures
                };
                localizationOptions.RequestCultureProviders = new List<IRequestCultureProvider>
                {
                    new CookieRequestCultureProvider(),
                    new RouteCultureProvider()
                };
                app.UseRequestLocalization(localizationOptions);

                app.Use(async (context, next) =>
                {
                    var routeCulture = context.GetRouteValue("culture") as string;
                    if (!string.IsNullOrWhiteSpace(routeCulture))
                    {
                        var feature = context.Features.Get<IRequestCultureFeature>();
                        var culture = feature?.RequestCulture.Culture.Name;
                        if (!string.IsNullOrWhiteSpace(culture))
                        {
                            var cookieValue = CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture));
                            context.Response.Cookies.Append(
                                CookieRequestCultureProvider.DefaultCookieName,
                                cookieValue,
                                new CookieOptions { IsEssential = true, Expires = DateTimeOffset.UtcNow.AddYears(1) });
                        }
                    }

                    await next();
                });

                app.UseAuthentication();
                app.UseAuthorization();

                ApplyMigrations(app);

                app.MapGet("/", (HttpContext context) =>
                {
                    var cookie = context.Request.Cookies[CookieRequestCultureProvider.DefaultCookieName];
                    var culture = CookieRequestCultureProvider.ParseCookieValue(cookie)?.Cultures.FirstOrDefault().Value;

                    var prefix = culture != null && culture.StartsWith("bg", StringComparison.OrdinalIgnoreCase)
                        ? "/bg"
                        : "/en";

                    return Results.Redirect(prefix);
                });

                app.MapControllers();

                app.MapControllerRoute(
                    name: "match-pin",
                    pattern: "m",
                    defaults: new { area = "Kiosk", controller = "MatchPin", action = "Index" });

                app.MapControllerRoute(
                    name: "public-area",
                    pattern: "{culture:regex(^bg|en$)}/{controller=Home}/{action=Index}/{id?}",
                    defaults: new { area = "Public" });

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

        private static void ApplyMigrations(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;

            var dataContext = services.GetRequiredService<DataContext>();
            dataContext.Database.Migrate();

            var identityContext = services.GetRequiredService<ApplicationDbContext>();
            identityContext.Database.Migrate();

            SeedAdminUser(services);
        }

        private static void SeedAdminUser(IServiceProvider services)
        {
            const string adminRole = "Administrator";
            const string adminEmail = "miroslav.braikov@gmail.com";
            const string adminPassword = "!@#qweASD1";

            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            if (!roleManager.RoleExistsAsync(adminRole).GetAwaiter().GetResult())
            {
                var roleResult = roleManager.CreateAsync(new IdentityRole(adminRole)).GetAwaiter().GetResult();
                if (!roleResult.Succeeded)
                {
                    Log.Warn($"Failed to seed role {adminRole}: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                }
            }

            var existingUser = userManager.FindByEmailAsync(adminEmail).GetAwaiter().GetResult();
            if (existingUser != null)
            {
                var alreadyAdmin = userManager.IsInRoleAsync(existingUser, adminRole).GetAwaiter().GetResult();
                if (!alreadyAdmin)
                {
                    var addRoleResult = userManager.AddToRoleAsync(existingUser, adminRole).GetAwaiter().GetResult();
                    if (!addRoleResult.Succeeded)
                    {
                        Log.Warn($"Failed to add user {adminEmail} to {adminRole}: {string.Join(", ", addRoleResult.Errors.Select(e => e.Description))}");
                    }
                }
                return;
            }

            var user = new IdentityUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true
            };

            var result = userManager.CreateAsync(user, adminPassword).GetAwaiter().GetResult();
            if (result.Succeeded)
            {
                var addRoleResult = userManager.AddToRoleAsync(user, adminRole).GetAwaiter().GetResult();
                if (!addRoleResult.Succeeded)
                {
                    Log.Warn($"Failed to add user {adminEmail} to {adminRole}: {string.Join(", ", addRoleResult.Errors.Select(e => e.Description))}");
                }
                Log.Info($"Seeded admin user {adminEmail}");
                return;
            }

            Log.Warn($"Failed to seed admin user {adminEmail}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

    }
}
