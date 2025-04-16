using Microsoft.EntityFrameworkCore;

namespace PerformansTakip.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Admin> Admins { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Student> Students { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Class)
                .WithMany(c => c.Students)
                .HasForeignKey(s => s.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure SQLite specific settings
            modelBuilder.Entity<Admin>()
                .Property(a => a.LastLogin)
                .HasColumnType("TEXT");

            modelBuilder.Entity<Student>()
                .Property(s => s.LastUpdated)
                .HasColumnType("TEXT");
        }
    }
} 