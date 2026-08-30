using Microsoft.AspNetCore.Identity;
using StudentServiceRequest.Web.Models.Domain;
using System.Collections.Generic;

namespace StudentServiceRequest.Web.Models.Identity;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
}