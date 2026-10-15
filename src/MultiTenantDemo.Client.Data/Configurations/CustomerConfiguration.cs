using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantDemo.Client.Domain;

namespace MultiTenantDemo.Client.Data.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers", "dbo");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).UseIdentityColumn();
        builder.Property(c => c.Name).HasMaxLength(Customer.NameMaxLength).IsRequired();
        builder.Property(c => c.Email).HasMaxLength(Customer.EmailMaxLength).IsRequired();
        builder.Property(c => c.IsActive).IsRequired();
        builder.Property(c => c.CreatedUtc).HasColumnType("datetime2(3)").IsRequired();
        builder.HasIndex(c => c.Email).IsUnique();
    }
}
