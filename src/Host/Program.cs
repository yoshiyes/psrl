using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Passerelle.Domain.Entities;
using Passerelle.Host;
using Passerelle.Host.Common;
using Passerelle.Host.Data;
using Serilog;

StaticLogger.EnsureInitialized();

try
{
    Log.Information("Starting Passerelle.");

    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.AddApp();

    WebApplication app = builder.Build();

    using (IServiceScope scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        db.Database.Migrate();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await DbSeeder.SeedAsync(db, userManager, roleManager, builder.Configuration, app.Environment);
    }

    app.UseApp();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Server terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}