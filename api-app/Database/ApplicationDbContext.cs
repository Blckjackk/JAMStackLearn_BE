using Microsoft.EntityFrameworkCore;
using api_app.Models;

namespace api_app.Database
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectUser> ProjectUsers { get; set; }
        public DbSet<ProjectInvite> ProjectInvites { get; set; }
        public DbSet<TaskItem> Tasks { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<TaskTag> TaskTags { get; set; }
        public DbSet<UserIdentity> UserIdentities { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.UserCode).IsRequired().HasMaxLength(20);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.UserCode).IsUnique();

                entity.HasMany(e => e.ProjectMemberships)
                    .WithOne(pu => pu.User)
                    .HasForeignKey(pu => pu.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.AssignedTasks)
                    .WithOne(t => t.AssigneeUser)
                    .HasForeignKey(t => t.AssigneeUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                entity.HasMany(e => e.Identities)
                    .WithOne(i => i.User)
                    .HasForeignKey(i => i.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure UserIdentity entity
            modelBuilder.Entity<UserIdentity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
                entity.Property(e => e.ProviderUserId).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Email).HasMaxLength(255);
                entity.Property(e => e.DisplayName).HasMaxLength(150);
                entity.Property(e => e.AvatarUrl).HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(e => new { e.Provider, e.ProviderUserId }).IsUnique();
            });

            // Configure Project entity
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasMany(e => e.Tasks)
                    .WithOne(t => t.Project)
                    .HasForeignKey(t => t.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Members)
                    .WithOne(pu => pu.Project)
                    .HasForeignKey(pu => pu.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ProjectUser entity (Many-to-Many join)
            modelBuilder.Entity<ProjectUser>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Role)
                    .IsRequired()
                    .HasMaxLength(50);
                entity.Property(e => e.JoinedAt).HasDefaultValueSql("GETUTCDATE()");

                // Unique constraint: one user per project
                entity.HasIndex(e => new { e.ProjectId, e.UserId }).IsUnique();

                entity.HasOne(e => e.Project)
                    .WithMany(p => p.Members)
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.ProjectMemberships)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure TaskItem entity
            modelBuilder.Entity<TaskItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("Tasks");
                entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Description).HasMaxLength(2000);
                entity.Property(e => e.Content).HasMaxLength(5000);
                entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("TODO");
                entity.Property(e => e.Priority).HasMaxLength(50).HasDefaultValue("Medium");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Project)
                    .WithMany(p => p.Tasks)
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.AssigneeUser)
                    .WithMany(u => u.AssignedTasks)
                    .HasForeignKey(e => e.AssigneeUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            // Configure Tag entity
            modelBuilder.Entity<Tag>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Color).HasMaxLength(7).HasDefaultValue("#3b82f6");
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasMany(e => e.Tasks)
                    .WithMany(t => t.Tags)
                    .UsingEntity<TaskTag>(
                        l => l.HasOne<TaskItem>().WithMany().HasForeignKey(tt => tt.TaskId),
                        r => r.HasOne<Tag>().WithMany().HasForeignKey(tt => tt.TagId)
                    );
            });

            // Configure TaskTag entity (Many-to-Many join)
            modelBuilder.Entity<TaskTag>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => new { e.TaskId, e.TagId }).IsUnique();

                entity.HasOne(e => e.Task)
                    .WithMany()
                    .HasForeignKey(e => e.TaskId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Tag)
                    .WithMany()
                    .HasForeignKey(e => e.TagId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure ProjectInvite entity
            modelBuilder.Entity<ProjectInvite>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Status).IsRequired().HasMaxLength(20);
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.Project)
                    .WithMany()
                    .HasForeignKey(e => e.ProjectId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.InvitedUser)
                    .WithMany()
                    .HasForeignKey(e => e.InvitedUserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.InvitedByUser)
                    .WithMany()
                    .HasForeignKey(e => e.InvitedByUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });
        }
    }
}
