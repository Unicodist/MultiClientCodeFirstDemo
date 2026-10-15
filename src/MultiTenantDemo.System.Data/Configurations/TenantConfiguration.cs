using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantDemo.System.Domain;

namespace MultiTenantDemo.System.Data.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants", SystemDbContext.Schema);
        builder.HasKey(t => t.TenantId).HasName("PK_Tenants");
        builder.Property(t => t.TenantId).HasMaxLength(64);
        builder.Property(t => t.Name).HasMaxLength(200).IsRequired();
        builder.Property(t => t.ConnectionString).HasMaxLength(1000).IsRequired();
        builder.Property(t => t.IsActive).IsRequired();
        builder.Property(t => t.CreatedUtc).HasColumnType("datetime2(3)").HasDefaultValueSql("SYSUTCDATETIME()");
    }
}
