using Microsoft.EntityFrameworkCore;
using MultiTenantDemo.Client.Data.Configurations;
using MultiTenantDemo.Client.Domain;

namespace MultiTenantDemo.Client.Data;

public class ClientDbContext : DbContext
{
    public ClientDbContext(DbContextOptions<ClientDbContext> options)
        : base(options)
    {
    }

    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
    }
}
