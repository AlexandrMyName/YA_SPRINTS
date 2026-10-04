using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SprintASP_NetCore_API.Domain.Entities;
using Microsoft.EntityFrameworkCore;


namespace SprintsASP_NetCore_API.Infrastructure.DataAccess.Configurations;


public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{

    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("refresh_tokens");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
        b.HasIndex(x => x.TokenHash).IsUnique();
        b.Property(x => x.ExpiresAt).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();

        b.HasOne<User>()
         .WithMany()
         .HasForeignKey(x => x.UserId)
         .OnDelete(DeleteBehavior.Cascade);
    }
}
