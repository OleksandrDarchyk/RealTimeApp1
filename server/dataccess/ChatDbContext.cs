using dataccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace dataccess;

public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options) { }

    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ----- Rooms -----
        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("rooms");

            entity.HasKey(r => r.Id);

            entity.Property(r => r.Id)
                .HasColumnName("id")
                .IsRequired();

            entity.Property(r => r.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");
        });

        // ----- Messages -----
        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");

            entity.HasKey(m => m.Id);

            entity.Property(m => m.Id)
                .HasColumnName("id");

            entity.Property(m => m.RoomId)
                .HasColumnName("room_id")
                .IsRequired();

            entity.Property(m => m.Content)
                .HasColumnName("content")
                .IsRequired();

            // "from" is a SQL keyword, so we store it as "from_name" in DB
            entity.Property(m => m.From)
                .HasColumnName("from_name");

            entity.Property(m => m.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("NOW()");

            // FK: messages.room_id -> rooms.id
            entity.HasOne(m => m.Room)
                .WithMany(r => r.Messages)
                .HasForeignKey(m => m.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for: "last N messages in a room"
            entity.HasIndex(m => new { m.RoomId, m.CreatedAt })
                .HasDatabaseName("ix_messages_room_createdat");
        });

        base.OnModelCreating(modelBuilder);
    }
}
