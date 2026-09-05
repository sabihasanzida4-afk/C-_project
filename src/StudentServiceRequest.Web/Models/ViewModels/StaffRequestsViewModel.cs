using StudentServiceRequest.Web.Models.Domain;

namespace StudentServiceRequest.Web.Models.ViewModels;

public class StaffRequestsViewModel
{
    public List<RequestListViewModel> Requests { get; set; } = new();
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public string? Search { get; set; }
    public int? StatusId { get; set; }
    public List<RequestStatus> Statuses { get; set; } = new();
}
