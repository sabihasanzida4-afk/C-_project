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
    private readonly ILogger<StaffController> _logger;

    public StaffController(AppDbContext context, ILogger<StaffController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> Index(string? search, int? statusId, int page = 1)
    {
        try
        {
            const int pageSize = 10;
            if (page < 1) page = 1;

            var query = _context.ServiceRequests
                .Include(sr => sr.Student)
                .Include(sr => sr.RequestType)
                .Include(sr => sr.Status)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(sr =>
                    (sr.Student != null && sr.Student.FullName.Contains(term)) ||
                    (sr.Student != null && sr.Student.Email != null && sr.Student.Email.Contains(term)) ||
                    sr.Description.Contains(term));
            }

            if (statusId.HasValue)
            {
                query = query.Where(sr => sr.StatusId == statusId.Value);
            }

            var totalCount = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
            if (page > totalPages) page = totalPages;

            var requests = await query
                .OrderByDescending(sr => sr.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var statuses = await _context.RequestStatuses.Where(rs => rs.IsActive).OrderBy(rs => rs.SortOrder).ToListAsync();

            var vm = new StaffRequestsViewModel
            {
                Requests = requests.Select(r => new RequestListViewModel
                {
                    Id = r.Id,
                    StudentName = r.Student?.FullName ?? "Unknown",
                    StudentEmail = r.Student?.Email ?? "-",
                    RequestTypeName = r.RequestType?.Name ?? "Unknown",
                    Description = string.IsNullOrEmpty(r.Description) ? "" : (r.Description.Length > 150 ? r.Description.Substring(0, 150) + "..." : r.Description),
                    StatusName = r.Status?.Name ?? "Unknown",
                    StatusCssClass = r.Status?.CssClass ?? "secondary",
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Staff/Index search={Search} statusId={StatusId} page={Page}", search, statusId, page);
            // Fallback: return empty view with error message instead of 500 Error page
            TempData["ErrorMessage"] = $"Unable to load requests: {ex.Message}";
            var statuses = new List<RequestStatus>();
            try { statuses = await _context.RequestStatuses.Where(rs => rs.IsActive).ToListAsync(); } catch { }
            return View(new StaffRequestsViewModel { Search = search, StatusId = statusId, Statuses = statuses });
        }
    }

    public async Task<IActionResult> Details(int? id)
    {
        try
        {
            if (id == null) return NotFound();

            var request = await _context.ServiceRequests
                .Include(sr => sr.Student)
                .Include(sr => sr.RequestType)
                .Include(sr => sr.Status)
                .FirstOrDefaultAsync(sr => sr.Id == id);

            if (request == null) return NotFound();

            var statuses = await _context.RequestStatuses.Where(rs => rs.IsActive).OrderBy(rs => rs.SortOrder).ToListAsync();

            var vm = new UpdateStatusViewModel
            {
                RequestId = request.Id,
                StatusId = request.StatusId,
                RequestTypeName = request.RequestType?.Name ?? "Unknown",
                StudentName = request.Student?.FullName ?? "Unknown",
                CurrentStatusName = request.Status?.Name ?? "Unknown",
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
                StudentName = request.Student?.FullName ?? "Unknown",
                StudentEmail = request.Student?.Email ?? "-",
                RequestTypeId = request.RequestTypeId,
                RequestTypeName = request.RequestType?.Name ?? "Unknown",
                RequestTypeDescription = request.RequestType?.Description ?? "",
                StatusId = request.StatusId,
                StatusName = request.Status?.Name ?? "Unknown",
                StatusCssClass = request.Status?.CssClass ?? "secondary",
                Description = request.Description,
                CreatedAt = request.CreatedAt,
                UpdatedAt = request.UpdatedAt
            };

            return View(vm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Staff/Details id={Id}", id);
            TempData["ErrorMessage"] = $"Unable to load request: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(UpdateStatusViewModel vm)
    {
        try
        {
            var request = await _context.ServiceRequests.FindAsync(vm.RequestId);
            if (request == null) return NotFound();

            request.StatusId = vm.StatusId;
            request.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Request status updated successfully!";
            return RedirectToAction(nameof(Details), new { id = vm.RequestId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Staff/UpdateStatus RequestId={RequestId} StatusId={StatusId}", vm.RequestId, vm.StatusId);
            TempData["ErrorMessage"] = $"Failed to update status: {ex.Message}";
            return RedirectToAction(nameof(Details), new { id = vm.RequestId });
        }
    }
}