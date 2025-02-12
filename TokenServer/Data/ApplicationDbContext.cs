using Microsoft.EntityFrameworkCore;
using TokenServer.Entities;

namespace TokenServer.Data;

public class ApplicationDbContext : DbContext {
    public DbSet<Account> Accounts { get; set; }
    public DbSet<Character> Characters { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        var connectionString = "Server=localhost;Port=3306;Database=aikaria;User=root;Password=Jose2904.;";
        optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString))
            .LogTo(Console.WriteLine, LogLevel.Information);
    }
}
