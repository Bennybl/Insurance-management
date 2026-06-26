using InsuranceManagement.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InsuranceManagement.Api.Infrastructure.Configurations;

public class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.ToTable("Policies");

        builder.HasKey(policy => policy.Id);

        builder.Property(policy => policy.PolicyNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(policy => policy.PolicyNumber)
            .IsUnique();

        builder.Property(policy => policy.ProductCode)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(policy => policy.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(policy => policy.StartDate)
            .IsRequired();

        builder.Property(policy => policy.EndDate)
            .IsRequired();

        builder.Property(policy => policy.PremiumAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(policy => policy.DetailsJson);

        builder.Property(policy => policy.CreatedAt)
            .IsRequired();

        builder.Property(policy => policy.CancellationReason)
            .HasMaxLength(500);

        builder.HasIndex(policy => policy.CustomerId);

        builder.HasIndex(policy => policy.ProductCode);

        builder.HasIndex(policy => policy.Status);

        builder.HasOne(policy => policy.Product)
            .WithMany(product => product.Policies)
            .HasForeignKey(policy => policy.ProductCode)
            .HasPrincipalKey(product => product.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
