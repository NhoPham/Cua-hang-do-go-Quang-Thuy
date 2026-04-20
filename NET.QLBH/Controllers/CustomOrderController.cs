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
    private readonly IWebHostEnvironment _environment;

    public CustomOrderController(QlbhContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
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

        ValidateReferenceImages(model.ReferenceImages);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var requestCode = await GenerateRequestCodeAsync();
        var uploadedPaths = await SaveReferenceImagesAsync(requestCode, model.ReferenceImages, HttpContext.RequestAborted);
        var mergedImages = MergeReferenceImages(model.ReferenceImageUrls, uploadedPaths);

        var request = new CustomOrderRequest
        {
            RequestCode = requestCode,
            UserId = GetCurrentUserId(),
            ProductId = model.ProductId,
            CustomerName = model.CustomerName.Trim(),
            Email = model.Email.Trim().ToLowerInvariant(),
            Phone = model.Phone.Trim(),
            RequestedProductName = model.RequestedProductName.Trim(),
            WoodType = string.IsNullOrWhiteSpace(model.WoodType) ? null : model.WoodType.Trim(),
            Dimensions = string.IsNullOrWhiteSpace(model.Dimensions) ? null : model.Dimensions.Trim(),
            Quantity = model.Quantity,
            EstimatedBudget = model.EstimatedBudget,
            DesiredDeliveryDate = model.DesiredDeliveryDate,
            Description = model.Description.Trim(),
            ReferenceImageUrls = mergedImages,
            Status = CustomOrderStatuses.New,
            CreatedAt = DateTime.UtcNow
        };

        _context.CustomOrderRequests.Add(request);
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

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Lookup()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Lookup(string code, string email)
    {
        code = (code ?? string.Empty).Trim();
        email = (email ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(email))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập mã yêu cầu và email.";
            return View();
        }

        var request = await _context.CustomOrderRequests
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.RequestCode == code && x.Email == email);

        if (request == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy yêu cầu phù hợp.";
            return View();
        }

        return RedirectToAction(nameof(Details), new { code = request.RequestCode, email = request.Email });
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Details(string code, string email)
    {
        code = (code ?? string.Empty).Trim();
        email = (email ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(email))
        {
            TempData["ErrorMessage"] = "Thiếu mã yêu cầu hoặc email.";
            return RedirectToAction(nameof(Lookup));
        }

        var request = await _context.CustomOrderRequests
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.RequestCode == code && x.Email == email);

        if (request == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy yêu cầu.";
            return RedirectToAction(nameof(Lookup));
        }

        return View(request);
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> MyDetails(int id)
    {
        var request = await _context.CustomOrderRequests
            .Include(x => x.Product)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (request == null)
        {
            return NotFound();
        }

        var userId = GetCurrentUserId();
        if (!User.IsInRole("admin") && (!userId.HasValue || request.UserId != userId.Value))
        {
            return Forbid();
        }

        return View("Details", request);
    }

    [Authorize(Roles = "admin")]
    [HttpGet]
    public async Task<IActionResult> Admin(string? status, string? keyword)
    {
        var normalizedStatus = string.IsNullOrWhiteSpace(status)
            ? null
            : CustomOrderUiHelper.NormalizeStatus(status);

        var query = _context.CustomOrderRequests
            .Include(x => x.Product)
            .Include(x => x.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(normalizedStatus))
        {
            query = query.Where(x => x.Status == normalizedStatus);
        }

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.RequestCode.ToLower().Contains(normalizedKeyword) ||
                x.CustomerName.ToLower().Contains(normalizedKeyword) ||
                x.Email.ToLower().Contains(normalizedKeyword) ||
                x.RequestedProductName.ToLower().Contains(normalizedKeyword));
        }

        ViewBag.Status = normalizedStatus;
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
        status = CustomOrderUiHelper.NormalizeStatus(status);

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
        return RedirectToAction(nameof(Admin), new { status });
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

    private void ValidateReferenceImages(IEnumerable<IFormFile>? files)
    {
        if (files == null)
        {
            return;
        }

        var allowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        var fileList = files.Where(x => x != null && x.Length > 0).ToList();
        if (fileList.Count > 5)
        {
            ModelState.AddModelError(nameof(CustomOrderRequestViewModel.ReferenceImages), "Bạn chỉ được tải tối đa 5 ảnh.");
        }

        foreach (var file in fileList)
        {
            var extension = Path.GetExtension(file.FileName);
            if (!allowedExtensions.Contains(extension))
            {
                ModelState.AddModelError(nameof(CustomOrderRequestViewModel.ReferenceImages), $"Định dạng {extension} không được hỗ trợ.");
            }

            if (file.Length > 5 * 1024 * 1024)
            {
                ModelState.AddModelError(nameof(CustomOrderRequestViewModel.ReferenceImages), $"Ảnh {file.FileName} vượt quá 5MB.");
            }
        }
    }

    private async Task<List<string>> SaveReferenceImagesAsync(string requestCode, IEnumerable<IFormFile>? files, CancellationToken cancellationToken)
    {
        var result = new List<string>();
        if (files == null)
        {
            return result;
        }

        var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "custom-orders", requestCode);
        Directory.CreateDirectory(uploadFolder);

        var index = 0;
        foreach (var file in files.Where(x => x.Length > 0))
        {
            index++;
            var extension = Path.GetExtension(file.FileName);
            var safeName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{index}{extension}";
            var filePath = Path.Combine(uploadFolder, safeName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream, cancellationToken);

            result.Add($"/uploads/custom-orders/{requestCode}/{safeName}");
        }

        return result;
    }

    private static string? MergeReferenceImages(string? rawUrls, IEnumerable<string> uploadedPaths)
    {
        var allImages = CustomOrderUiHelper.ParseReferenceImageUrls(rawUrls)
            .Concat(uploadedPaths)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return allImages.Any() ? string.Join(Environment.NewLine, allImages) : null;
    }
}
