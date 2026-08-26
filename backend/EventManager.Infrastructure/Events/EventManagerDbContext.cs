using EventManager.Domain.Events.Entities;

using Microsoft.EntityFrameworkCore;

namespace EventManager.Infrastructure.Events;

public sealed class EventManagerDbContext(DbContextOptions<EventManagerDbContext> options)
    : DbContext(options)
{
    public DbSet<Event> Events => Set<Event>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events", table =>
            {
                table.HasCheckConstraint("CK_Events_Capacity", "[Capacity] > 0");
                table.HasCheckConstraint("CK_Events_Price", "[Price] >= 0");
            });

            entity.HasKey(eventEntity => eventEntity.Id);
            entity.Property(eventEntity => eventEntity.Title).HasMaxLength(200).IsRequired();
            entity.Property(eventEntity => eventEntity.Description).HasMaxLength(2000).IsRequired();
            entity.Property(eventEntity => eventEntity.Location).HasMaxLength(200).IsRequired();
            entity.Property(eventEntity => eventEntity.Category).HasMaxLength(50).IsRequired();
            entity.Property(eventEntity => eventEntity.ArtistName).HasMaxLength(200);
            entity.Property(eventEntity => eventEntity.Price).HasPrecision(10, 2);
            entity.HasIndex(eventEntity => eventEntity.Date);
            entity.HasIndex(eventEntity => eventEntity.Category);
        });
    }
}