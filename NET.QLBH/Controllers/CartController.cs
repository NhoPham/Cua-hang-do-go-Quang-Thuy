using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLBH.Models;
using QLBH.Utils;
using QLBH.ViewModels;

namespace QLBH.Controllers;

[Authorize]
public class CartController : Controller
{
    private readonly QlbhContext _context;

    public CartController(QlbhContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Challenge();
        }

        var items = await _context.CartItems
            .Include(x => x.Product)
            .Where(x => x.UserId == userId.Value)
            .OrderByDescending(x => x.Id)
            .ToListAsync();

        var vm = new CartViewModel
        {
            Items = items.Select(x => new CartLineViewModel
            {
                CartItemId = x.Id,
                ProductId = x.ProductId,
                ProductName = x.Product?.Name ?? "Sản phẩm đã xóa",
                ProductImage = ImageHelper.GetFirstImageOrDefault(x.Product?.Images),
                UnitPrice = x.UnitPrice,
                Quantity = x.Quantity,
                MaxStock = x.Product?.Stock ?? 0
            }).ToList()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity = 1, string? returnUrl = null)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Challenge();
        }

        if (quantity <= 0)
        {
            quantity = 1;
        }

        var product = await _context.Products.FindAsync(productId);
        if (product == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
            return RedirectToAction("Index", "Product");
        }

        if (product.Stock <= 0)
        {
            TempData["ErrorMessage"] = "Sản phẩm hiện đã hết hàng.";
            return RedirectBack(returnUrl, productId);
        }

        var existingItem = await _context.CartItems
            .FirstOrDefaultAsync(x => x.UserId == userId.Value && x.ProductId == productId);

        if (existingItem == null)
        {
            _context.CartItems.Add(new CartItem
            {
                UserId = userId.Value,
                ProductId = productId,
                Quantity = Math.Min(quantity, product.Stock),
                UnitPrice = product.Price,
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existingItem.Quantity = Math.Min(existingItem.Quantity + quantity, product.Stock);
            existingItem.UnitPrice = product.Price;
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã thêm sản phẩm vào giỏ hàng.";
        return RedirectBack(returnUrl, productId);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity(int id, int quantity)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Challenge();
        }

        var item = await _context.CartItems
            .Include(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId.Value);

        if (item == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sản phẩm trong giỏ hàng.";
            return RedirectToAction(nameof(Index));
        }

        if (quantity <= 0)
        {
            _context.CartItems.Remove(item);
        }
        else
        {
            var maxStock = item.Product?.Stock ?? quantity;
            item.Quantity = Math.Min(quantity, Math.Max(maxStock, 1));
            item.UnitPrice = item.Product?.Price ?? item.UnitPrice;
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã cập nhật giỏ hàng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Challenge();
        }

        var item = await _context.CartItems
            .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId.Value);

        if (item != null)
        {
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi giỏ hàng.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear()
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Challenge();
        }

        var items = await _context.CartItems
            .Where(x => x.UserId == userId.Value)
            .ToListAsync();

        if (items.Any())
        {
            _context.CartItems.RemoveRange(items);
            await _context.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = "Đã xóa toàn bộ giỏ hàng.";
        return RedirectToAction(nameof(Index));
    }

    private IActionResult RedirectBack(string? returnUrl, int productId)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Details", "Product", new { id = productId });
    }
}
