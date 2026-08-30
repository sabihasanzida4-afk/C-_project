using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentServiceRequest.Web.Data;
using StudentServiceRequest.Web.Models.Domain;
using StudentServiceRequest.Web.Models.ViewModels;

namespace StudentServiceRequest.Web.Controllers;

[Authorize(Roles = "Staff")]
public class StaffController : Controller
{
    private readonly AppDbContext _context;

    public StaffController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(string? search, int? statusId, int page = 1)
    {
        const int pageSize = 10;

        var query = _context.ServiceRequests
            .Include(sr => sr.Student)
            .Include(sr => sr.RequestType)
            .Include(sr => sr.Status)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(sr =>
                sr.Student.FullName.Contains(search) ||
                sr.Student.Email!.Contains(search) ||
                sr.Description.Contains(search));
        }

        if (statusId.HasValue)
        {
            query = query.Where(sr => sr.StatusId == statusId.Value);
        }

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var requests = await query
            .OrderByDescending(sr => sr.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var statuses = await _context.RequestStatuses.Where(rs => rs.IsActive).ToListAsync();

        var vm = new
        {
            Requests = requests.Select(r => new RequestListViewModel
            {
                Id = r.Id,
                StudentName = r.Student.FullName,
                StudentEmail = r.Student.Email!,
                RequestTypeName = r.RequestType.Name,
                Description = r.Description.Length > 150 ? r.Description.Substring(0, 150) + "..." : r.Description,
                StatusName = r.Status.Name,
                StatusCssClass = r.Status.CssClass,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            }).ToList(),
            CurrentPage = page,
            TotalPages = totalPages,
            Search = search,
            StatusId = statusId,
            Statuses = statuses
        };

        return View(vm);
    }

    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var request = await _context.ServiceRequests
            .Include(sr => sr.Student)
            .Include(sr => sr.RequestType)
            .Include(sr => sr.Status)
            .FirstOrDefaultAsync(sr => sr.Id == id);

        if (request == null) return NotFound();

        var statuses = await _context.RequestStatuses.Where(rs => rs.IsActive).ToListAsync();

        var vm = new UpdateStatusViewModel
        {
            RequestId = request.Id,
            StatusId = request.StatusId,
            RequestTypeName = request.RequestType.Name,
            StudentName = request.Student.FullName,
            CurrentStatusName = request.Status.Name,
            Statuses = statuses.Select(s => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Name,
                Selected = s.Id == request.StatusId
            })
        };

        ViewBag.Request = new RequestDetailViewModel
        {
            Id = request.Id,
            StudentId = request.StudentId,
            StudentName = request.Student.FullName,
            StudentEmail = request.Student.Email!,
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(UpdateStatusViewModel vm)
    {
        var request = await _context.ServiceRequests.FindAsync(vm.RequestId);
        if (request == null) return NotFound();

        request.StatusId = vm.StatusId;
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Request status updated successfully!";
        return RedirectToAction(nameof(Details), new { id = vm.RequestId });
    }
}