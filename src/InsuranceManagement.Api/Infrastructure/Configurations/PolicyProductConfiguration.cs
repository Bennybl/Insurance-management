using InsuranceManagement.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InsuranceManagement.Api.Infrastructure.Configurations;

public sealed class PolicyProductConfiguration : IEntityTypeConfiguration<PolicyProduct>
{
    public void Configure(EntityTypeBuilder<PolicyProduct> builder)
    {
        builder.ToTable("PolicyProducts");

        builder.HasKey(product => product.Code);

        builder.Property(product => product.Code)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(product => product.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(product => product.IsActive)
            .IsRequired();

        builder.HasData(
            new PolicyProduct { Code = "CAR", Name = "Car Insurance", IsActive = true },
            new PolicyProduct { Code = "HEALTH", Name = "Health Insurance", IsActive = true },
            new PolicyProduct { Code = "LIFE", Name = "Life Insurance", IsActive = true },
            new PolicyProduct { Code = "HOME", Name = "Home Insurance", IsActive = true },
            new PolicyProduct { Code = "TRAVEL", Name = "Travel Insurance", IsActive = true });
    }
}
