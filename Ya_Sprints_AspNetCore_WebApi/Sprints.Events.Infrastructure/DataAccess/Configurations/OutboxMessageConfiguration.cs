using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sprints.Events.Domain.Entities;


namespace Sprints.Events.Infrastructure.DataAccess.Configurations;


public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> b)
    {
        b.ToTable("outbox_messages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();

        b.Property(x => x.Type).IsRequired().HasMaxLength(100);
        b.Property(x => x.Topic).IsRequired().HasMaxLength(100);
        b.Property(x => x.Key).IsRequired().HasMaxLength(200);
        b.Property(x => x.Payload).IsRequired().HasColumnType("jsonb");
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.ProcessedAt);
        b.Property(x => x.Attempts).IsRequired();
        b.Property(x => x.LastError).HasMaxLength(2000);

        // Индекс для быстрого поиска необработанных
        b.HasIndex(x => new { x.ProcessedAt, x.CreatedAt });
    }
}