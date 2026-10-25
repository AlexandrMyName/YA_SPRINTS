using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sprints.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;


namespace Sprints.Events.Infrastructure.DataAccess.Configurations;


public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> b)
    {
        b.ToTable("inbox_messages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Type).IsRequired().HasMaxLength(100);
        b.Property(x => x.ReceivedAt).IsRequired();
        b.Property(x => x.ProcessedAt).IsRequired();
    }
}
