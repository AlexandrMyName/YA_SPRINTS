using Microsoft.EntityFrameworkCore.Metadata.Builders;  
using SprintASP_NetCore_API.Domain.Entities;
using Microsoft.EntityFrameworkCore;


namespace SprintsASP_NetCore_API.Infrastructure.DataAccess.Configurations;


public class EventConfiguration : IEntityTypeConfiguration<Event>
{

    public void Configure(EntityTypeBuilder<Event> builder)
    {

        builder.ToTable("events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
       
        // Настройка свойства Version для использования xmin
        // builder.Property(e => e.Version).IsRowVersion();
        builder.Property<uint>("xmin") .IsRowVersion();

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description) 
            .HasMaxLength(1000);

        builder.Property(e => e.StartAt)
            .IsRequired();
        builder.Property(e => e.EndAt)
         .IsRequired();


        builder.Property(e => e.TotalSeats).IsRequired(); 
        builder.Property(e => e.AvailableSeats).IsRequired();

        // Связь один-ко-многим: Event имеет много Booking
        builder.HasMany(e => e.Bookings)
            .WithOne(b => b.Event)
            .HasForeignKey(b => b.EventId)
            .OnDelete(DeleteBehavior.Cascade); // по умолчанию и так стоит
    }
}
