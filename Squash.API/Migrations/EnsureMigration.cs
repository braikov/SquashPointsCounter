using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Squash.API.Migrations
{
    public static class EnsureMigration
    {
        public static void EnsureMigrationOfContext<T>(this IApplicationBuilder app) where T : DbContext
        {
            using var scope = app.ApplicationServices.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<T>();

            var db = service.Database;

            try
            {
                db.Migrate();
            }
            catch (Exception e)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<T>>();
                logger.LogError(e, "Failed to run migration for {context}: {message}", typeof(T), e.Message);

                throw;
            }
        }
    }
}
