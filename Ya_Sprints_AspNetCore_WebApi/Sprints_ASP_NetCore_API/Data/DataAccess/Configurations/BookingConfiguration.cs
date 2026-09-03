using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SprintASP_NetCore_API.Data.Entities;
using Microsoft.EntityFrameworkCore;


namespace SprintASP_NetCore_API.Data.DataAccess.Configurations;


public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{

    public void Configure(EntityTypeBuilder<Booking> builder)
    {

        builder.ToTable("bookings");

        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        //// Настройка свойства Version для использования xmin
        //builder.Property(e => e.Version).IsRowVersion();
        builder.Property<uint>("xmin").IsRowVersion();
         
        builder.Property(b => b.CreatedAt) .IsRequired();

        builder.Property(b => b.ProcessedAt);
         
        // Храним Enum как строку
        builder.Property(b => b.Status)
            .IsRequired()
            .HasConversion<string>();

        // настройка связей
        builder.HasOne(b => b.Event)                // У Booking есть один Event
            .WithMany(e => e.Bookings)              // У Event есть много Booking
            .HasForeignKey(b => b.EventId)           
            .OnDelete(DeleteBehavior.Cascade);     

    }
}