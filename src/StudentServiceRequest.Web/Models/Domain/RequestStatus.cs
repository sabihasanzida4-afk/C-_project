namespace StudentServiceRequest.Web.Models.Domain;

public class RequestStatus
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CssClass { get; set; } = "secondary";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ServiceRequest> Requests { get; set; } = new List<ServiceRequest>();
}