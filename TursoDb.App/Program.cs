using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TursoDb.App.Data;
using TursoDb.App.Models;
using TursoDb.Data;

namespace TursoDb.App;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== TursoDb - EF Core + Turso (HTTP API) ===");
        Console.WriteLine();

        // Load configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var tursoOptions = new TursoOptions();
        configuration.GetSection("Turso").Bind(tursoOptions);

        // Auth token is always read from environment variable TURSO_AUTH_TOKEN
        tursoOptions.AuthToken = Environment.GetEnvironmentVariable("TURSO_AUTH_TOKEN") ?? string.Empty;

        // Allow TURSO_DATABASE_URL to override config file
        var envUrl = Environment.GetEnvironmentVariable("TURSO_DATABASE_URL");
        if (!string.IsNullOrWhiteSpace(envUrl))
            tursoOptions.DatabaseUrl = envUrl;

        if (string.IsNullOrWhiteSpace(tursoOptions.DatabaseUrl) ||
            string.IsNullOrWhiteSpace(tursoOptions.AuthToken))
        {
            Console.WriteLine("ERROR: Turso database URL and auth token are required.");
            Console.WriteLine();
            Console.WriteLine("Set them via:");
            Console.WriteLine("  1. appsettings.json (Turso:DatabaseUrl)");
            Console.WriteLine("  2. Environment variable TURSO_DATABASE_URL");
            Console.WriteLine("  3. Environment variable TURSO_AUTH_TOKEN (required)");
            Console.WriteLine();
            Console.WriteLine("Get your values from:");
            Console.WriteLine("  turso db show <db-name> --http-url");
            Console.WriteLine("  turso db tokens create <db-name>");
            return;
        }

        // Configure EF Core with Turso HTTP
        var optionsBuilder = new DbContextOptionsBuilder<TursoDbContext>();
        optionsBuilder.UseTurso(tursoOptions.DatabaseUrl, tursoOptions.AuthToken);
        optionsBuilder.LogTo(Console.WriteLine, Microsoft.Extensions.Logging.LogLevel.Information);
        optionsBuilder.EnableSensitiveDataLogging();

        await using var db = new TursoDbContext(optionsBuilder.Options);

        // Ensure the table exists
        await EnsureSchemaAsync(db);

        // Run demo CRUD operations
        await RunDemoAsync(db);
    }

    static async Task EnsureSchemaAsync(TursoDbContext db)
    {
        Console.WriteLine("Ensuring database schema...");
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS tours (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                description TEXT,
                destination TEXT NOT NULL,
                price REAL NOT NULL DEFAULT 0,
                duration_days INTEGER NOT NULL DEFAULT 1,
                is_active INTEGER NOT NULL DEFAULT 1,
                created_at TEXT NOT NULL,
                updated_at TEXT
            )";
        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine("Schema ready.");
        Console.WriteLine();
    }

    static async Task RunDemoAsync(TursoDbContext db)
    {
        // Clean up old data for a fresh demo
        await db.Database.ExecuteSqlRawAsync("DELETE FROM tours");

        // ===== CREATE =====
        Console.WriteLine("--- CREATE: Adding tours ---");

        var tours = new List<Tour>
        {
            new()
            {
                Name = "Paris City Break",
                Description = "Explore the City of Light with guided tours of the Eiffel Tower, Louvre, and Notre-Dame.",
                Destination = "Paris, France",
                Price = 1299.99,
                DurationDays = 5,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Name = "Tokyo Adventure",
                Description = "Experience the vibrant culture of Tokyo, from Shibuya Crossing to ancient temples.",
                Destination = "Tokyo, Japan",
                Price = 2499.50,
                DurationDays = 10,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Name = "Safari in Kenya",
                Description = "Witness the Great Migration on a once-in-a-lifetime safari adventure.",
                Destination = "Nairobi, Kenya",
                Price = 3899.00,
                DurationDays = 7,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Name = "Northern Lights Iceland",
                Description = "Chase the aurora borealis across Iceland's stunning landscapes.",
                Destination = "Reykjavik, Iceland",
                Price = 1899.75,
                DurationDays = 6,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            }
        };

        db.Tours.AddRange(tours);
        var created = await db.SaveChangesAsync();
        Console.WriteLine($"Created {created} tours.");
        Console.WriteLine();

        // ===== READ ALL =====
        Console.WriteLine("--- READ: All tours ---");
        var allTours = await db.Tours.ToListAsync();
        foreach (var tour in allTours)
        {
            Console.WriteLine($"  {tour}");
        }
        Console.WriteLine();

        // ===== READ FILTERED =====
        Console.WriteLine("--- READ: Active tours under $2000 ---");
        var affordableTours = await db.Tours
            .Where(t => t.IsActive && t.Price < 2000)
            .ToListAsync();
        affordableTours = affordableTours.OrderBy(t => t.Price).ToList();
        foreach (var tour in affordableTours)
        {
            Console.WriteLine($"  {tour}");
        }
        Console.WriteLine();

        // ===== READ SINGLE =====
        Console.WriteLine("--- READ: Single tour by ID ---");
        var firstTour = await db.Tours.FirstOrDefaultAsync();
        if (firstTour != null)
        {
            Console.WriteLine($"  Found: {firstTour}");
        }
        Console.WriteLine();

        // ===== UPDATE =====
        Console.WriteLine("--- UPDATE: Modifying a tour ---");
        if (firstTour != null)
        {
            firstTour.Price = 1499.99;
            firstTour.Description += " (Updated with new pricing!)";
            firstTour.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            Console.WriteLine($"  Updated tour [{firstTour.Id}] - New price: ${firstTour.Price:F2}");

            var updated = await db.Tours.FindAsync(firstTour.Id);
            Console.WriteLine($"  Verified: {updated}");
        }
        Console.WriteLine();

        // ===== DELETE =====
        Console.WriteLine("--- DELETE: Removing a tour ---");
        var toDelete = await db.Tours.FirstOrDefaultAsync(t => !t.IsActive);
        if (toDelete != null)
        {
            db.Tours.Remove(toDelete);
            await db.SaveChangesAsync();
            Console.WriteLine($"  Deleted tour [{toDelete.Id}] {toDelete.Name}");
        }
        Console.WriteLine();

        // ===== FINAL STATE =====
        Console.WriteLine("--- FINAL STATE: Remaining tours ---");
        var remaining = await db.Tours.OrderBy(t => t.Id).ToListAsync();
        foreach (var tour in remaining)
        {
            Console.WriteLine($"  {tour}");
        }
        Console.WriteLine();
        Console.WriteLine($"Total tours in database: {remaining.Count}");
        Console.WriteLine();
        Console.WriteLine("=== Demo complete! ===");
    }
}
