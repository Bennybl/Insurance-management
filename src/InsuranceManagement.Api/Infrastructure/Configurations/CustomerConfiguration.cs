using InsuranceManagement.Api.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InsuranceManagement.Api.Infrastructure.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(customer => customer.LastName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(customer => customer.Email)
            .HasMaxLength(320)
            .IsRequired();

        builder.HasIndex(customer => customer.Email)
            .IsUnique();

        builder.Property(customer => customer.PhoneNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(customer => customer.Address)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(customer => customer.IsActive)
            .IsRequired();

        builder.Property(customer => customer.CreatedAt)
            .IsRequired();

        builder.HasMany(customer => customer.Policies)
            .WithOne(policy => policy.Customer)
            .HasForeignKey(policy => policy.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
