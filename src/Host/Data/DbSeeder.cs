using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Passerelle.Domain.Entities;

namespace Passerelle.Host.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        const string adminRole = "Admin";

        if (!await roleManager.RoleExistsAsync(adminRole))
            await roleManager.CreateAsync(new IdentityRole(adminRole));
        
        string adminEmail = configuration["UserAdmin:Email"] ?? "admin@linkshare.local";
        string adminPassword = configuration["UserAdmin:Password"] ?? "Admin12345";
        string adminUsername = "admin";

        ApplicationUser? admin = await userManager.FindByNameAsync(adminUsername);

        if (admin is not null)
        {
            bool emailChanged = !string.Equals(admin.Email, adminEmail, StringComparison.OrdinalIgnoreCase);
            bool passwordChanged = !await userManager.CheckPasswordAsync(admin, adminPassword);

            if (emailChanged || passwordChanged)
            {
                IdentityResult deleteResult = await userManager.DeleteAsync(admin);
                ThrowIfFailed(deleteResult, "Unable to delete existing admin user");
                
                admin = null;
            }
        }

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = adminUsername,
                Email = adminEmail,
                EmailConfirmed = true
            };

            IdentityResult createResult = await userManager.CreateAsync(admin, adminPassword);
            ThrowIfFailed(createResult, "Unable to create admin user");
            
            IdentityResult roleResult = await userManager.AddToRoleAsync(admin, adminRole);
            ThrowIfFailed(roleResult, "Unable to add admin user to Admin role");
        }
        
        if (!environment.IsDevelopment()) return;

        if (await db.Categories.AnyAsync()) return;

        SeedDataDto seedData = await LoadSeedDataAsync(environment);

        var categoriesBySlug = seedData.Categories.ToDictionary(
            categoryDto => categoryDto.Slug,
            categoryDto => new Category { Name = categoryDto.Name, Slug = categoryDto.Slug, Emoji = categoryDto.Emoji }
        );

        db.Categories.AddRange(categoriesBySlug.Values);
        await db.SaveChangesAsync();

        var links = seedData.Links
            .Select(linkDto => new Link { Title = linkDto.Title, Url = linkDto.Url, Description = linkDto.Description })
            .ToArray();

        db.Links.AddRange(links);
        await db.SaveChangesAsync();

        var linkCategories = seedData.Links
            .Zip(links, (linkDto, link) => (linkDto, link))
            .SelectMany(pair => pair.linkDto.Categories.Select(slug =>
                new LinkCategory { LinkId = pair.link.Id, CategoryId = categoriesBySlug[slug].Id }));

        db.LinkCategories.AddRange(linkCategories);

        await db.SaveChangesAsync();
    }

    private static async Task<SeedDataDto> LoadSeedDataAsync(IWebHostEnvironment environment)
    {
        string path = Path.Combine(environment.ContentRootPath, "sample_links.json");

        await using FileStream stream = File.OpenRead(path);

        return await JsonSerializer.DeserializeAsync<SeedDataDto>(stream)
               ?? throw new InvalidOperationException($"Unable to read seed data from '{path}'.");
    }

    private sealed record SeedDataDto(List<SeedCategoryDto> Categories, List<SeedLinkDto> Links);

    private sealed record SeedCategoryDto(string Slug, string Name, string? Emoji);

    private sealed record SeedLinkDto(string Title, string Url, string Description, List<string> Categories);

    private static void ThrowIfFailed(IdentityResult result, string message)
    {
        if (result.Succeeded) return;

        string errors = string.Join(
            Environment.NewLine,
            result.Errors.Select(error => $"- {error.Code}: {error.Description}")
        );

        throw new InvalidOperationException($"{message}:{Environment.NewLine}{errors}");
    }
}