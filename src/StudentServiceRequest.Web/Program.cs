using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StudentServiceRequest.Web.Data;
using StudentServiceRequest.Web.Models.Identity;
using StudentServiceRequest.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Supports both Npgsql key=value and postgresql:// URL formats (Neon, Render DATABASE_URL).
// Falls back to the requested Neon pooler URL if no config/env is set.
static string ConvertPostgresUrlToNpgsql(string url)
{
    var uri = new Uri(url);
    var userInfo = uri.UserInfo.Split(':', 2);
    var username = Uri.UnescapeDataString(userInfo[0]);
    var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
    var host = uri.Host;
    var port = uri.Port > 0 ? uri.Port : 5432;
    var database = uri.AbsolutePath.TrimStart('/').Split('?')[0].Split('/')[0];
    // Parse query string manually (avoid System.Web dependency)
    var queryParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    if (!string.IsNullOrEmpty(uri.Query))
    {
        foreach (var kv in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = kv.Split('=', 2);
            var k = Uri.UnescapeDataString(parts[0]);
            var v = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : "";
            queryParams[k] = v;
        }
    }
    var csb = new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = host,
        Port = port,
        Database = string.IsNullOrEmpty(database) ? "neondb" : database,
        Username = username,
        Password = password,
    };
    if (queryParams.TryGetValue("sslmode", out var sslMode) && Enum.TryParse<Npgsql.SslMode>(sslMode, true, out var parsedSslMode))
        csb.SslMode = parsedSslMode;
    else
        csb.SslMode = Npgsql.SslMode.Require;

    if (queryParams.TryGetValue("channel_binding", out var cb) && Enum.TryParse<Npgsql.ChannelBinding>(cb, true, out var parsedCb))
        csb.ChannelBinding = parsedCb;

    // Preserve additional params like pooling, timeout, etc. if present
    foreach (var kv in queryParams)
    {
        if (kv.Key.Equals("sslmode", StringComparison.OrdinalIgnoreCase) || kv.Key.Equals("channel_binding", StringComparison.OrdinalIgnoreCase))
            continue;
        // Let Npgsql handle known keys; ignore unknown
        try { csb[kv.Key] = kv.Value; } catch { }
    }
    return csb.ConnectionString;
}

string GetResolvedConnectionString(IConfiguration config)
{
    // Priority: env DATABASE_URL (Render/Neon) > ConnectionStrings__DefaultConnection > appsettings DefaultConnection > hardcoded fallback
    var raw = Environment.GetEnvironmentVariable("DATABASE_URL")
           ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
           ?? config.GetConnectionString("DefaultConnection")
           ?? "postgresql://neondb_owner:npg_ZtLvDRB3p1Yw@ep-solitary-wildflower-a50c6mgr-pooler.us-east-2.aws.neon.tech/neondb?sslmode=require&channel_binding=require";

    if (raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) || raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        return ConvertPostgresUrlToNpgsql(raw);
    return raw;
}

var connectionString = GetResolvedConnectionString(builder.Configuration);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Set to false so dummy EmailSender (logs only) doesn't block login.
    // Original was true which caused "Invalid login attempt" for unconfirmed emails since no real email is sent.
    // For production with real email provider, set back to true.
    options.SignIn.RequireConfirmedEmail = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

var app = builder.Build();

// Seed database - resilient for Render/Neon pooled connections
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<AppDbContext>();

        // Log pending vs applied for diagnostics (helps when __EFMigrationsHistory is out of sync)
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        logger.LogInformation("Applied migrations: {Applied}", appliedMigrations.Any() ? string.Join(", ", appliedMigrations) : "(none)");
        logger.LogInformation("Pending migrations: {Pending}", pendingMigrations.Any() ? string.Join(", ", pendingMigrations) : "(none)");

        if (pendingMigrations.Any())
        {
            logger.LogInformation("Running database migrations...");
            // Use async migrate. On Neon pooled connections ( -pooler host) PgBouncer
            // transaction mode doesn't support advisory locks correctly. Prefer non-pooled host for migrations.
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migrations completed.");
        }
        else
        {
            logger.LogInformation("No pending migrations. Database is already up to date.");
            // Safety check: verify critical Identity table actually exists.
            // Handles corrupted __EFMigrationsHistory (e.g., row exists but tables were never created
            // due to PgBouncer/transaction-pooling or manual DB wipe).
            try
            {
                var canQueryRoles = await context.Database.ExecuteSqlRawAsync("SELECT 1 FROM \"AspNetRoles\" LIMIT 1") >= 0;
                logger.LogInformation("Schema verification: AspNetRoles exists and is queryable.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Schema verification failed: __EFMigrationsHistory says up to date but AspNetRoles is missing. "
                    + "This happens on Neon when using the pooled (-pooler) host for migrations or when __EFMigrationsHistory was corrupted. "
                    + "Fix: 1) Switch connection string to NON-pooled host (remove -pooler, e.g. ep-xxx.us-east-2.aws.neon.tech) for migrations, "
                    + "2) Delete the corrupted history row: DELETE FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '20260905063155_InitialCreate', then restart, "
                    + "or 3) Drop and recreate schema if safe: DROP SCHEMA public CASCADE; CREATE SCHEMA public;");
                // Don't rethrow here - let SeedData fail with a clear log instead of obscure crash loop
            }
        }

        await SeedData.InitializeAsync(services);
        logger.LogInformation("Database seeding completed.");
    }
    catch (Exception ex)
    {
        // Don't crash the whole container (exit 139 / PostgresException 42P01) on seed failure.
        // Log and continue so /healthz can report and Render doesn't crash-loop.
        // For critical startup failures, Render health checks will still fail.
        logger.LogError(ex, "Error during database migration/seeding");
        // Optionally rethrow in Development to fail fast:
        if (app.Environment.IsDevelopment())
            throw;
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHealthChecks("/healthz");

app.Run();