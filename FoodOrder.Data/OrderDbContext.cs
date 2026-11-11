using System.Text.Json;
using FoodOrder.Domain;
using Microsoft.EntityFrameworkCore;

namespace FoodOrder.Data;

public class OrderDbContext : DbContext
{
    private readonly string? _sqlitePath;

    // Standard constructor for DI or custom options (e.g. SQL Server)
    public OrderDbContext(DbContextOptions<OrderDbContext> options) : base(options) { }

    // Backward-compatible constructor for SQLite file path (used by WPF / local console)
    public OrderDbContext(string sqlitePath)
    {
        _sqlitePath = sqlitePath;
    }

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        // If options weren't configured via DbContextOptions (e.g. in WPF), fall back to SQLite
        if (!options.IsConfigured && !string.IsNullOrEmpty(_sqlitePath))
        {
            options.UseSqlite($"Data Source={_sqlitePath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
            .Property(o => o.Items)
            .HasConversion(
                items => JsonSerializer.Serialize(items, (JsonSerializerOptions?)null),
                json => JsonSerializer.Deserialize<List<MenuItem>>(json, (JsonSerializerOptions?)null)
                        ?? new List<MenuItem>());
    }

    // Factory helper for SQL Server
    public static OrderDbContext ForSqlServer(string connectionString)
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new OrderDbContext(options);
    }
}