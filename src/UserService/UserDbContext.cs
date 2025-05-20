
using Microsoft.EntityFrameworkCore;
using MyBuildingBlocks.Models.User;

public class UserDbContext : DbContext
{
    public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
    {
        
    }

    public DbSet<User> Users { get; set; }
    public DbSet<UserProfile> UserProfiles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasKey(u => u.Id);


        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<UserProfile>()
            .HasKey(up => up.Id);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Profile)
            .WithOne(up => up.User)
            .HasForeignKey<UserProfile>(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);




        base.OnModelCreating(modelBuilder);
    }
}