using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLBH.Models;
using QLBH.ViewModels;

namespace QLBH.Controllers;

[Authorize(Roles = "admin")]
public class InventoryController : Controller
{
    private readonly QlbhContext _context;

    public InventoryController(QlbhContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var soldLookup = await _context.OrderItems
            .Where(x => x.Order.OrderStatus != "CANCELLED")
            .GroupBy(x => x.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .ToDictionaryAsync(x => x.ProductId, x => x.Quantity);

        var products = await _context.Products
            .OrderBy(x => x.Stock)
            .ThenBy(x => x.Name)
            .ToListAsync();

        var vm = new InventoryDashboardViewModel
        {
            Products = products.Select(x => new InventoryProductRowViewModel
            {
                ProductId = x.Id,
                ProductName = x.Name,
                Price = x.Price,
                Stock = x.Stock,
                SoldQuantity = soldLookup.GetValueOrDefault(x.Id),
                IsLowStock = x.Stock <= 5
            }).ToList(),
            RecentTransactions = await _context.InventoryTransactions
                .Include(x => x.Product)
                .OrderByDescending(x => x.CreatedAt)
                .Take(20)
                .ToListAsync()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdjustStock(InventoryAdjustmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Dữ liệu điều chỉnh kho không hợp lệ.";
            return RedirectToAction(nameof(Index));
        }

        var product = await _context.Products.FindAsync(model.ProductId);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        var isImport = string.Equals(model.Type, "IMPORT", StringComparison.OrdinalIgnoreCase);
        var delta = isImport ? model.Quantity : -model.Quantity;

        if (!isImport && product.Stock < model.Quantity)
        {
            TempData["ErrorMessage"] = "Số lượng xuất kho vượt quá tồn kho hiện tại.";
            return RedirectToAction(nameof(Index));
        }

        product.Stock += delta;

        _context.InventoryTransactions.Add(new InventoryTransaction
        {
            ProductId = product.Id,
            QuantityChanged = delta,
            QuantityAfter = product.Stock,
            Type = isImport ? "MANUAL_IMPORT" : "MANUAL_EXPORT",
            Note = string.IsNullOrWhiteSpace(model.Note)
                ? (isImport ? "Nhập kho thủ công" : "Xuất kho thủ công")
                : model.Note.Trim(),
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã cập nhật tồn kho.";
        return RedirectToAction(nameof(Index));
    }
}
