namespace StudentServiceRequest.Web.Models.ViewModels;

public class StaffDashboardViewModel
{
    public int TotalRequests { get; set; }
    public int PendingRequests { get; set; }
    public int ProcessingRequests { get; set; }
    public int CompletedRequests { get; set; }
    public int RejectedRequests { get; set; }
    public List<RequestListViewModel> RecentRequests { get; set; } = new();
}