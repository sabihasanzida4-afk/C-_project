using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentServiceRequest.Web.Data;
using StudentServiceRequest.Web.Models.Domain;
using StudentServiceRequest.Web.Models.Identity;
using StudentServiceRequest.Web.Models.ViewModels;

namespace StudentServiceRequest.Web.Controllers;

[Authorize(Roles = "Student")]
public class StudentController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public StudentController(AppDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    private async Task<ApplicationUser?> GetCurrentUserAsync() =>
        await _userManager.GetUserAsync(User);

    public async Task<IActionResult> Dashboard()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Challenge();

        var requests = await _context.ServiceRequests
            .Where(sr => sr.StudentId == user.Id)
            .Include(sr => sr.RequestType)
            .Include(sr => sr.Status)
            .OrderByDescending(sr => sr.CreatedAt)
            .ToListAsync();

        var vm = new StudentDashboardViewModel
        {
            TotalRequests = requests.Count,
            PendingRequests = requests.Count(r => r.StatusId == 1),
            ProcessingRequests = requests.Count(r => r.StatusId == 2),
            CompletedRequests = requests.Count(r => r.StatusId == 3),
            RejectedRequests = requests.Count(r => r.StatusId == 4),
            RecentRequests = requests.Take(5).Select(r => new RequestListViewModel
            {
                Id = r.Id,
                RequestTypeName = r.RequestType.Name,
                Description = r.Description.Length > 100 ? r.Description.Substring(0, 100) + "..." : r.Description,
                StatusName = r.Status.Name,
                StatusCssClass = r.Status.CssClass,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            }).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var types = await _context.RequestTypes
            .Where(rt => rt.IsActive)
            .Select(rt => new { rt.Id, rt.Name })
            .ToListAsync();

        var vm = new SubmitRequestViewModel
        {
            RequestTypes = types.Select(t => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = t.Id.ToString(),
                Text = t.Name
            })
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SubmitRequestViewModel vm)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Challenge();

        if (ModelState.IsValid)
        {
            var request = new ServiceRequest
            {
                StudentId = user.Id,
                RequestTypeId = vm.RequestTypeId,
                Description = vm.Description,
                StatusId = 1 // Pending
            };

            _context.Add(request);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Your service request has been submitted successfully!";
            return RedirectToAction(nameof(MyRequests));
        }

        var types = await _context.RequestTypes
            .Where(rt => rt.IsActive)
            .Select(rt => new { rt.Id, rt.Name })
            .ToListAsync();

        vm.RequestTypes = types.Select(t => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
        {
            Value = t.Id.ToString(),
            Text = t.Name
        });

        return View(vm);
    }

    public async Task<IActionResult> MyRequests()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return Challenge();

        var requests = await _context.ServiceRequests
            .Where(sr => sr.StudentId == user.Id)
            .Include(sr => sr.RequestType)
            .Include(sr => sr.Status)
            .OrderByDescending(sr => sr.CreatedAt)
            .ToListAsync();

        var vm = requests.Select(r => new RequestListViewModel
        {
            Id = r.Id,
            RequestTypeName = r.RequestType.Name,
            Description = r.Description,
            StatusName = r.Status.Name,
            StatusCssClass = r.Status.CssClass,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        }).ToList();

        return View(vm);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var user = await GetCurrentUserAsync();
        if (user == null) return Challenge();

        var request = await _context.ServiceRequests
            .Include(sr => sr.RequestType)
            .Include(sr => sr.Status)
            .FirstOrDefaultAsync(sr => sr.Id == id && sr.StudentId == user.Id);

        if (request == null) return NotFound();

        var vm = new RequestDetailViewModel
        {
            Id = request.Id,
            StudentId = request.StudentId,
            StudentName = user.FullName,
            StudentEmail = user.Email!,
            RequestTypeId = request.RequestTypeId,
            RequestTypeName = request.RequestType.Name,
            RequestTypeDescription = request.RequestType.Description,
            StatusId = request.StatusId,
            StatusName = request.Status.Name,
            StatusCssClass = request.Status.CssClass,
            Description = request.Description,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt
        };

        return View(vm);
    }
}