namespace StudentServiceRequest.Web.Models.ViewModels;

public class RequestListViewModel
{
    public int Id { get; set; }
    public string RequestTypeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string StatusCssClass { get; set; } = "secondary";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
}