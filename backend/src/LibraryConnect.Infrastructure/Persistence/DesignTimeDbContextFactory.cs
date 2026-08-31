using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LibraryConnect.Infrastructure.Persistence;

/// <summary>
/// Used only by <c>dotnet ef</c> at design time. It builds the context directly so creating a
/// migration does not require the API host, its JWT secret or a running Redis/MinIO.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LibraryConnectDbContext>
{
    public LibraryConnectDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("LC_DESIGN_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=libraryconnect;Username=libraryconnect;Password=libraryconnect";

        var options = new DbContextOptionsBuilder<LibraryConnectDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "sys"))
            .Options;

        return new LibraryConnectDbContext(options);
    }
}
