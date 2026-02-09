using dataccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace dataccess;

public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options) { }

    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<User> Users => Set<User>();

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

        // ----- Users -----
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(u => u.Id);

            entity.Property(u => u.Id)
                .HasColumnName("id")
                .IsRequired();

            entity.Property(u => u.Nickname)
                .HasColumnName("nickname")
                .IsRequired();

            entity.Property(u => u.Hash)
                .HasColumnName("hash")
                .IsRequired();

            entity.Property(u => u.Salt)
                .HasColumnName("salt")
                .IsRequired();

            entity.Property(u => u.Role)
                .HasColumnName("role")
                .IsRequired();

            // Unique username
            entity.HasIndex(u => u.Nickname)
                .IsUnique()
                .HasDatabaseName("ux_users_nickname");
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
