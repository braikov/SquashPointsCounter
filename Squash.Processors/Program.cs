using log4net;
using log4net.Config;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Squash.DataAccess;
using Squash.Shared.Logging;
using Squash.SqlServer;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using static System.Formats.Asn1.AsnWriter;

namespace Squash.Processors
{
    public partial class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()!.DeclaringType!);

        public static IConfigurationRoot _configuration = null!;

        public Program()
        {
#if DEVELOPMENT
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
#elif RELEASE
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Release");
#elif TEST
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Test");
#endif

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environment}.{Environment.MachineName}.json", optional: true, reloadOnChange: true)
                .Build();

            var connectionString = _configuration.GetConnectionString("DataContext");
            Log4NetTableEnsurer.Ensure(connectionString ?? string.Empty);

            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly()!);
            var log4NetConfigFile = new FileInfo($"log4net.{environment}.{Environment.MachineName}.config");
            if (!log4NetConfigFile.Exists)
            {
                log4NetConfigFile = new FileInfo($"log4net.{environment}.config");
            }
            XmlConfigurator.Configure(logRepository, log4NetConfigFile);

            //var c = _configuration.GetConnectionString("DataContext");
            //Console.WriteLine(c.ToString());
        }
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += GlobalExceptionHandler;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

            Console.WriteLine($"Processors version {Assembly.GetExecutingAssembly().GetName().Version}");
            var program = new Program();
            ApplyMigrations();
            program.CallMethodByName(args);
        }

        private static void GlobalExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Error("Unhandled exception occurred", e.ExceptionObject as Exception);
            Environment.Exit(1);
        }

        private static void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            Log.Error("Unobserved task exception occurred", e.Exception);
            e.SetObserved();
        }

        protected void CallMethodByName(string[] args)
        {
            if (args.Length != 0)
            {
                StringBuilder sb = null!;
                try
                {
                    MethodBase? method = GetType().GetMethod(args[0], BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

                    if (method != null)
                    {
                        var p = method.GetParameters();

                        object[]? parameters =
                            // if there is a method with one parameter, which is string array, use this signature
                            p.Length == 1
                            && p[0].ParameterType.IsArray && p[0].ParameterType.GetElementType() == typeof(string) ?
                                new[] { args.Skip(1).ToArray() }
                                // all other cases use null param
                                : null;


                        if (p.Length == 0 || parameters != null)
                        {
                            int start = Environment.TickCount;
                            try
                            {
                                object? result = method.Invoke(this, parameters);
                                if (result is StringBuilder builder)
                                    sb = builder;

                                MethodSucceseded(method, start, sb);
                                //Log.Info($"{args[0]} finished successfuly in {Environment.TickCount - start} mills! {sb?.ToString()}");
                                return;
                            }
                            catch (Exception ex)
                            {
                                MethodFailed(method, ex.InnerException ?? ex, start, sb);
                                //Log.Error($"{args[0]} Fatal Error in {Environment.TickCount - start} mills!! {(string.IsNullOrEmpty(ex.Message) ? ex.InnerException.Message : ex.Message)}", ex);
                            }
                            return;
                        }
                        else
                        {
                            Log.Error($"{Assembly.GetEntryAssembly()?.ManifestModule.Name} : Method {args[0]} found, but no suitable override found!");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"{Assembly.GetEntryAssembly()?.ManifestModule.Name}: Method not found. {ex.Message}.");
                }
            }
            Usage();
        }

        protected virtual void MethodSucceseded(MethodBase method, int startTime, StringBuilder sbResult)
            => Log.Info($"{method.Name} finished successfuly in {Environment.TickCount - startTime} mills! {sbResult?.ToString()}");

        protected virtual void MethodFailed(MethodBase method, Exception ex, int startTime, StringBuilder sbResult)
            => Log.Error($"{method.Name} Fatal Error in {Environment.TickCount - startTime} mills!! {(string.IsNullOrEmpty(ex.Message) ? ex.InnerException?.Message : ex.Message)}", ex);

        protected virtual void MethodNotAllowed(MethodBase method, string reason) => Log.Error(reason);

        protected static void Usage() => Console.WriteLine($@"
            {Assembly.GetEntryAssembly()?.ManifestModule.Name} method 
                or 
            {Assembly.GetEntryAssembly()?.ManifestModule.Name} method param1 .... paramn 
            ");

        /// <summary>
        /// Get a new the IDataContext for the application.
        /// </summary>
        /// <returns></returns>
#pragma warning disable IDE0079 // Remove unnecessary suppression
        [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance",
            Justification = "IDataContext should used everywhere instead of concrete implementation DataContext")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
        static IDataContext GetDataContext()
        {
            // Create DbContextOptions
            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
            optionsBuilder.UseSqlServer(_configuration?.GetConnectionString("DataContext"), x => x.UseCompatibilityLevel(100));

            return new DataContext(optionsBuilder.Options);
        }

        private static void ApplyMigrations()
        {
            var optionsBuilder = new DbContextOptionsBuilder<DataContext>();
            optionsBuilder.UseSqlServer(_configuration?.GetConnectionString("DataContext"), x => x.UseCompatibilityLevel(100));

            using var context = new DataContext(optionsBuilder.Options);
            try
            {
                context.Database.Migrate();
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}
