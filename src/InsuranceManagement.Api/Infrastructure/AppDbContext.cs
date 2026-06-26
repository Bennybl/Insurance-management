using InsuranceManagement.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace InsuranceManagement.Api.Infrastructure;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Policy> Policies => Set<Policy>();

    public DbSet<PolicyProduct> PolicyProducts => Set<PolicyProduct>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
