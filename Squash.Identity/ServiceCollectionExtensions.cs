// File: Squash.Identity/ServiceCollectionExtensions.cs
using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Squash.Identity
{
    public static class ServiceCollectionExtensions
    {
        // 1) Core Identity + DbContext (shared)
        public static IServiceCollection SetupIdentityCore<TUser, TRole>(this IServiceCollection services,
            IConfiguration configuration,
            string connectionString)
            where TUser : class
            where TRole : class
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.MigrationsHistoryTable("__IdentityMigrationsHistory");
                }));

            services.AddIdentity<TUser, TRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
                // copy other shared Identity options here
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            return services;
        }

        // 2) API-specific: JWT as the default auth scheme
        public static IServiceCollection SetupApiAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var key = configuration["Jwt:Key"];
            var issuer = configuration["Jwt:Issuer"];
            var audience = configuration["Jwt:Audience"];

            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience))
            {
                throw new InvalidOperationException("JWT configuration missing (Jwt:Key, Jwt:Issuer, Jwt:Audience).");
            }

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = false; // set true in production
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidIssuer = issuer,
                    IssuerSigningKey = signingKey
                };
            });

            return services;
        }

        // 3) Web-specific: configure Identity cookie (keep cookies as default auth scheme)
        public static IServiceCollection SetupWebAuthentication(this IServiceCollection services, Action<Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationOptions>? configureCookie = null)
        {
            // AddIdentity already registered cookie handlers via AddIdentity.
            // Use ConfigureApplicationCookie to tune cookie options.
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.Name = ".Squash.Auth";
                options.LoginPath = "/Identity/Account/Login";
                options.LogoutPath = "/Identity/Account/Logout";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromDays(14);
                options.SlidingExpiration = true;

                configureCookie?.Invoke(options);
            });

            // Ensure authentication middleware is aware of cookies as default (usually set by AddIdentity),
            // but explicitly configuring won't hurt:
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = IdentityConstants.ApplicationScheme;
                options.DefaultChallengeScheme = IdentityConstants.ApplicationScheme;
            });

            return services;
        }
    }
}
