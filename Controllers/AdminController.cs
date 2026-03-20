using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLBH.Models;
using QLBH.ViewModels;

namespace QLBH.Controllers;

[Authorize(Roles = "admin")]
public class AdminController : Controller
{
    private readonly QlbhContext _context;

    public AdminController(QlbhContext context)
    {
        _context = context;
    }

    // ===== ĐÃ THÊM: trang quản lý khách hàng =====
    [HttpGet]
    public async Task<IActionResult> Customers(string? keyword, string? roleFilter)
    {
        var users = _context.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim().ToLower();
            users = users.Where(x =>
                x.Username.ToLower().Contains(normalizedKeyword) ||
                x.Email.ToLower().Contains(normalizedKeyword));
        }

        if (!string.IsNullOrWhiteSpace(roleFilter))
        {
            users = users.Where(x => (x.Role ?? "customer") == roleFilter);
        }

        var model = await users
            .OrderByDescending(x => x.Id)
            .Select(x => new AdminCustomerViewModel
            {
                Id = x.Id,
                Username = x.Username,
                Email = x.Email,
                Role = string.IsNullOrWhiteSpace(x.Role) ? "customer" : x.Role!,
                OrderCount = x.Orders.Count(),
                TotalSpent = x.Orders.Sum(o => (decimal?)o.TotalAmount) ?? 0m
            })
            .ToListAsync();

        ViewBag.Keyword = keyword;
        ViewBag.RoleFilter = roleFilter;

        return View(model);
    }

    // ===== ĐÃ THÊM: đổi vai trò khách hàng/admin =====
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCustomerRole(int id, string role, string? keyword, string? roleFilter)
    {
        role = (role ?? string.Empty).Trim().ToLower();

        if (role != "admin" && role != "customer")
        {
            TempData["ErrorMessage"] = "Vai trò không hợp lệ.";
            return RedirectToAction(nameof(Customers), new { keyword, roleFilter });
        }

        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy tài khoản.";
            return RedirectToAction(nameof(Customers), new { keyword, roleFilter });
        }

        // ===== ĐÃ chặn tự đổi role của chính mình ở màn này =====
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == user.Id.ToString())
        {
            TempData["ErrorMessage"] = "Bạn không thể tự đổi vai trò của chính mình tại màn này.";
            return RedirectToAction(nameof(Customers), new { keyword, roleFilter });
        }

        if ((user.Role ?? "customer") == role)
        {
            TempData["InfoMessage"] = "Tài khoản này đã có đúng vai trò đó.";
            return RedirectToAction(nameof(Customers), new { keyword, roleFilter });
        }

        user.Role = role;
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã cập nhật vai trò cho tài khoản {user.Username}.";
        return RedirectToAction(nameof(Customers), new { keyword, roleFilter });
    }
}