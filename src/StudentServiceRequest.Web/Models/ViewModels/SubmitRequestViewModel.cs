using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StudentServiceRequest.Web.Models.ViewModels;

public class SubmitRequestViewModel
{
    [Required(ErrorMessage = "Please select a request type")]
    [Display(Name = "Request Type")]
    public int RequestTypeId { get; set; }

    [Required(ErrorMessage = "Description is required")]
    [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters", MinimumLength = 10)]
    [Display(Name = "Description")]
    [DataType(DataType.MultilineText)]
    public string Description { get; set; } = string.Empty;

    public IEnumerable<SelectListItem> RequestTypes { get; set; } = Enumerable.Empty<SelectListItem>();
}