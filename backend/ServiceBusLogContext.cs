using Microsoft.EntityFrameworkCore;
using servicebusapi2.Models;

public class ServiceBusLogContext : DbContext
{
    public DbSet<ServiceBusLog> ServiceBusLogs { get; set; }

    public ServiceBusLogContext(DbContextOptions<ServiceBusLogContext> options)
        : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<ServiceBusLog>();
        e.HasIndex(l => l.Timestamp);
        e.HasIndex(l => l.EventType);
        e.HasIndex(l => l.Severity);
    }
}
