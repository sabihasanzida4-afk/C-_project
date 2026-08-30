using StudentServiceRequest.Web.Models.Identity;

namespace StudentServiceRequest.Web.Models.Domain;

public class ServiceRequest
{
    public int Id { get; set; }

    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser Student { get; set; } = null!;

    public int RequestTypeId { get; set; }
    public RequestType RequestType { get; set; } = null!;

    public int StatusId { get; set; }
    public RequestStatus Status { get; set; } = null!;

    public string Description { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}