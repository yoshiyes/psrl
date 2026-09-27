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

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), o =>
                o.UseNodaTime()));

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
}