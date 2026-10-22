using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace NgoLeThanhToan_2380602271.Models;

public class ApplicationDbContext : IdentityDbContext<User>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<ClassRoom> Classes => Set<ClassRoom>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<ClassSession> Sessions => Set<ClassSession>();
    public DbSet<Attendance> Attendances => Set<Attendance>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);
        b.Entity<ClassRoom>().HasIndex(c => c.Code).IsUnique();
        b.Entity<Enrollment>().HasIndex(e => new { e.ClassRoomId, e.StudentId }).IsUnique();
        b.Entity<Attendance>().HasIndex(a => new { a.SessionId, a.StudentId }).IsUnique();
    }
}