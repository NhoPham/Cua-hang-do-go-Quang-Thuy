using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLBH.Models;
using QLBH.Utils;
using QLBH.ViewModels;

namespace QLBH.Controllers;

public class CustomOrderController : Controller
{
    private readonly QlbhContext _context;

    public CustomOrderController(QlbhContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Create(int? productId)
    {
        var vm = new CustomOrderRequestViewModel
        {
            Quantity = 1,
            DesiredDeliveryDate = DateTime.Today.AddDays(14)
        };

        if (User.Identity?.IsAuthenticated == true)
        {
            vm.CustomerName = User.Identity?.Name ?? string.Empty;
            vm.Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        }

        if (productId.HasValue)
        {
            var product = await _context.Products.FirstOrDefaultAsync(x => x.Id == productId.Value);
            if (product != null)
            {
                vm.ProductId = product.Id;
                vm.ProductName = product.Name;
                vm.ProductImage = ImageHelper.GetFirstImageOrDefault(product.Images);
                vm.RequestedProductName = product.Name;
                vm.Description = $"Tôi muốn đặt theo yêu cầu dựa trên mẫu {product.Name}.";
            }
        }

        return View(vm);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomOrderRequestViewModel model)
    {
        if (model.ProductId.HasValue)
        {
            var product = await _context.Products.FirstOrDefaultAsync(x => x.Id == model.ProductId.Value);
            if (product != null)
            {
                model.ProductName = product.Name;
                model.ProductImage = ImageHelper.GetFirstImageOrDefault(product.Images);
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var request = new CustomOrderRequest
        {
            RequestCode = await GenerateRequestCodeAsync(),
            UserId = GetCurrentUserId(),
            ProductId = model.ProductId,
            CustomerName = model.CustomerName.Trim(),
            Email = model.Email.Trim().ToLower(),
            Phone = model.Phone.Trim(),
            RequestedProductName = model.RequestedProductName.Trim(),
            WoodType = string.IsNullOrWhiteSpace(model.WoodType) ? null : model.WoodType.Trim(),
            Dimensions = string.IsNullOrWhiteSpace(model.Dimensions) ? null : model.Dimensions.Trim(),
            Quantity = model.Quantity,
            EstimatedBudget = model.EstimatedBudget,
            DesiredDeliveryDate = model.DesiredDeliveryDate,
            Description = model.Description.Trim(),
            ReferenceImageUrls = string.IsNullOrWhiteSpace(model.ReferenceImageUrls) ? null : model.ReferenceImageUrls.Trim(),
            Status = "new",
            CreatedAt = DateTime.UtcNow
        };

        _context.Add(request);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Success), new { code = request.RequestCode });
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Success(string code)
    {
        ViewBag.RequestCode = code;
        return View();
    }

    [Authorize(Roles = "admin")]
    [HttpGet]
    public async Task<IActionResult> Admin(string? status, string? keyword)
    {
        var query = _context.CustomOrderRequests
            .Include(x => x.Product)
            .Include(x => x.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status.Trim().ToLower());
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalized = keyword.Trim().ToLower();
            query = query.Where(x =>
                x.RequestCode.ToLower().Contains(normalized) ||
                x.CustomerName.ToLower().Contains(normalized) ||
                x.Email.ToLower().Contains(normalized) ||
                x.RequestedProductName.ToLower().Contains(normalized));
        }

        ViewBag.Status = status;
        ViewBag.Keyword = keyword;

        var data = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return View(data);
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, string? adminNote)
    {
        var allowedStatuses = new[] { "new", "consulting", "quoted", "approved", "cancelled" };
        status = (status ?? string.Empty).Trim().ToLower();

        if (!allowedStatuses.Contains(status))
        {
            TempData["ErrorMessage"] = "Trạng thái yêu cầu không hợp lệ.";
            return RedirectToAction(nameof(Admin));
        }

        var request = await _context.CustomOrderRequests.FirstOrDefaultAsync(x => x.Id == id);
        if (request == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy yêu cầu.";
            return RedirectToAction(nameof(Admin));
        }

        request.Status = status;
        request.AdminNote = string.IsNullOrWhiteSpace(adminNote) ? null : adminNote.Trim();
        request.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Đã cập nhật yêu cầu {request.RequestCode}.";

        return RedirectToAction(nameof(Admin));
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(claim, out var userId) ? userId : null;
    }

    private async Task<string> GenerateRequestCodeAsync()
    {
        while (true)
        {
            var code = $"YC{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";
            var exists = await _context.CustomOrderRequests.AnyAsync(x => x.RequestCode == code);
            if (!exists)
            {
                return code;
            }
        }
    }
}
