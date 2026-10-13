using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SprintASP_NetCore_API.Domain.Entities;
using Microsoft.EntityFrameworkCore;


namespace SprintsASP_NetCore_API.Infrastructure.DataAccess.Configurations;


public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.Login).IsRequired().HasMaxLength(50);
        b.HasIndex(x => x.Login).IsUnique();

        b.Property(x => x.PasswordHash).IsRequired().HasMaxLength(128);
        b.Property(x => x.Role).HasConversion<string>().IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
    }
}