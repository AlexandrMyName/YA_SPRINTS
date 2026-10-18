using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;  
using Sprints.Bookings.Domain.Entities; 


namespace Sprints.Bookings.Infrastructure.DataAccess.Configurations;


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
         
         

    }
}