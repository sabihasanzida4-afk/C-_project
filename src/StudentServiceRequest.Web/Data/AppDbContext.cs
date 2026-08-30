using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudentServiceRequest.Web.Models.Domain;
using StudentServiceRequest.Web.Models.Identity;

namespace StudentServiceRequest.Web.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<RequestType> RequestTypes => Set<RequestType>();
    public DbSet<RequestStatus> RequestStatuses => Set<RequestStatus>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ServiceRequest>(entity =>
        {
            entity.HasOne(sr => sr.Student)
                .WithMany(u => u.ServiceRequests)
                .HasForeignKey(sr => sr.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(sr => sr.RequestType)
                .WithMany(rt => rt.Requests)
                .HasForeignKey(sr => sr.RequestTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(sr => sr.Status)
                .WithMany(rs => rs.Requests)
                .HasForeignKey(sr => sr.StatusId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(sr => sr.StudentId);
            entity.HasIndex(sr => sr.StatusId);
            entity.HasIndex(sr => sr.CreatedAt);
        });

        builder.Entity<RequestType>(entity =>
        {
            entity.HasData(
                new RequestType { Id = 1, Name = "ID Card Replacement", Description = "Request for replacement of lost or damaged student ID card", IsActive = true },
                new RequestType { Id = 2, Name = "Transcript Request", Description = "Request for official academic transcript", IsActive = true },
                new RequestType { Id = 3, Name = "Certificate Request", Description = "Request for enrollment, graduation, or other certificates", IsActive = true }
            );
        });

        builder.Entity<RequestStatus>(entity =>
        {
            entity.HasData(
                new RequestStatus { Id = 1, Name = "Pending", Description = "Request submitted, awaiting review", CssClass = "warning", SortOrder = 1, IsActive = true },
                new RequestStatus { Id = 2, Name = "Processing", Description = "Request is being processed by staff", CssClass = "info", SortOrder = 2, IsActive = true },
                new RequestStatus { Id = 3, Name = "Completed", Description = "Request has been completed", CssClass = "success", SortOrder = 3, IsActive = true },
                new RequestStatus { Id = 4, Name = "Rejected", Description = "Request has been rejected", CssClass = "danger", SortOrder = 4, IsActive = true }
            );
        });

        builder.Entity<IdentityRole>().HasData(
            new IdentityRole { Id = "1", Name = "Student", NormalizedName = "STUDENT", ConcurrencyStamp = "1" },
            new IdentityRole { Id = "2", Name = "Staff", NormalizedName = "STAFF", ConcurrencyStamp = "2" }
        );
    }
}