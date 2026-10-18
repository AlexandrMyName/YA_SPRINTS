using Microsoft.EntityFrameworkCore.Metadata.Builders;  
using Sprints.Events.Domain.Entities;
using Microsoft.EntityFrameworkCore;


namespace Sprints.Events.Infrastructure.DataAccess.Configurations;


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
         
    }
}
