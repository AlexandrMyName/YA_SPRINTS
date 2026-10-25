using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sprints.Users.Domain.Entities;

namespace Sprints.Users.Infrastructure.DataAccess.Configurations;


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

        // Bootstrap-админ для dev
        // login: admin
        // password: admin
        // PasswordHash = SHA-256("admin")
        b.HasData(new
        {
            Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Login = "admin",
            PasswordHash = "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918",
            Role = UserRole.Admin,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}