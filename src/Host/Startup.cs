using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Passerelle.Domain.Entities;
using Passerelle.Host.Data;
using Passerelle.Host.Interfaces;
using Passerelle.Host.Repositories;
using Serilog;

namespace Passerelle.Host;

public static class Startup
{
    public static void AddApp(this IHostApplicationBuilder builder)
    {
        builder.Services.AddSerilog((services, lc) => lc
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services));

        builder.UseDatabase();

        builder.Services.AddScoped<ILinkRepository, LinkRepository>();
        builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();

        builder.Services.AddLocalization();

        builder.Services.AddControllersWithViews()
            .AddViewLocalization()
            .AddDataAnnotationsLocalization(options =>
                options.DataAnnotationLocalizerProvider = (_, factory) =>
                    factory.Create(typeof(SharedResource)));

        builder.Services.AddRazorPages();
        builder.Services.AddHealthChecks();

        builder.Services.Configure<RequestLocalizationOptions>(options =>
        {
            string[] supportedCultures = ["fr", "en"];

            options.SetDefaultCulture("fr")
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures);

            options.ApplyCurrentCultureToResponseHeaders = true;
        });

        builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 10;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
                options.SignIn.RequireConfirmedAccount = false;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = builder.Configuration["Authentication:LoginPath"];
            options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            options.Cookie.Name = builder.Configuration["Authentication:CookieName"];
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };

            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });
    }

    public static void UseApp(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseRequestLocalization(
            app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapAreaControllerRoute(
            name: "Admin",
            areaName: "Admin",
            pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
        app.MapControllerRoute(
            name: "auth",
            pattern: $"{app.Configuration["Authentication:AuthRoute"]}/{{action=Index}}/{{id?}}",
            defaults: new { controller = "Auth" });
        app.MapControllerRoute(
            "default",
            pattern: "{controller:regex(^(?!Auth$).*)=Home}/{action=Index}/{id?}");
        app.MapRazorPages();
        app.UseHealthChecks("/health");
    }

    private static void UseDatabase(this IHostApplicationBuilder builder)
    {
        string provider = builder.Configuration["DatabaseProvider"] ?? "";
        string connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";

        bool isSqlite = provider.Equals("SQLite", StringComparison.OrdinalIgnoreCase)
                        || connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)
                        || connectionString.EndsWith(".db", StringComparison.OrdinalIgnoreCase)
                        || connectionString.EndsWith(".sqlite", StringComparison.OrdinalIgnoreCase);

        if (isSqlite)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                connectionString = "Data Source=data/passerelle.db";

            EnsureSqliteDirectoryExists(connectionString);

            builder.Services.AddDbContext<ApplicationDbContext, SqliteApplicationDbContext>(options =>
                options.UseSqlite(connectionString, o => o.UseNodaTime()));
            
            Log.Information("Use SQLite database");
        }
        else
        {
            builder.Services.AddDbContext<ApplicationDbContext, PostgreSqlApplicationDbContext>(options =>
                options.UseNpgsql(connectionString, o => o.UseNodaTime()));
            
            Log.Information("Use PostgreSql database");
        }
    }

    private static void EnsureSqliteDirectoryExists(string connectionString)
    {
        try
        {
            Match match = Regex.Match(connectionString, @"Data Source=([^;]+)", RegexOptions.IgnoreCase);

            if (!match.Success) return;
            
            string path = match.Groups[1].Value.Trim();

            if (string.IsNullOrEmpty(path) || path == ":memory:") return;
            
            string? dir = Path.GetDirectoryName(path);
            
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
        catch
        {
            // Ignore if connection string cannot be parsed as a file path
        }
    }
}