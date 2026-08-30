using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StudentServiceRequest.Web.Models.ViewModels;

public class UpdateStatusViewModel
{
    [Required]
    public int RequestId { get; set; }

    [Required(ErrorMessage = "Please select a status")]
    [Display(Name = "Status")]
    public int StatusId { get; set; }

    public IEnumerable<SelectListItem> Statuses { get; set; } = Enumerable.Empty<SelectListItem>();

    // For display purposes
    public string RequestTypeName { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string CurrentStatusName { get; set; } = string.Empty;
}