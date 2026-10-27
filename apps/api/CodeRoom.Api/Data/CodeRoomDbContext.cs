namespace CodeRoom.Api.Data;

using Microsoft.EntityFrameworkCore;
using CodeRoom.Api.ProjectFiles;
using CodeRoom.Api.Projects;
using CodeRoom.Api.Collaboration;
public class CodeRoomDbContext(DbContextOptions<CodeRoomDbContext> options) : DbContext(options)
{
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectFile> ProjectFiles { get; set; }
    public DbSet<CollaborativeDocumentUpdate> CollaborativeDocumentUpdates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>()
            .HasIndex(p => p.Name)
            .IsUnique();

        modelBuilder.Entity<ProjectFile>()
            .HasIndex(pf => new { pf.ProjectId, pf.Path })
            .IsUnique();

        modelBuilder.Entity<ProjectFile>()
            .HasOne(pf => pf.Project)
            .WithMany(p => p.ProjectFiles)
            .HasForeignKey(pf => pf.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProjectFile>()
            .Property(pf => pf.Path)
            .HasMaxLength(500);

        modelBuilder.Entity<CollaborativeDocumentUpdate>()
            .HasOne(cd => cd.ProjectFile)
            .WithMany(pf => pf.CollaborativeDocumentUpdates)
            .HasForeignKey(cd => cd.FileId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CollaborativeDocumentUpdate>()
            .Property(u => u.CreatedAt)
            .HasDefaultValueSql("now()");

        modelBuilder.Entity<CollaborativeDocumentUpdate>()
            .HasIndex(u => new { u.FileId, u.UpdateId }).IsUnique();

    }
}
