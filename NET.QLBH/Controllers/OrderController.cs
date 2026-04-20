using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLBH.Models;
using QLBH.Utils;
using QLBH.ViewModels;

namespace QLBH.Controllers;

public class OrderController : Controller
{
    private readonly QlbhContext _context;

    public OrderController(QlbhContext context)
    {
        _context = context;
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Challenge();
        }

        var cartVm = await BuildCheckoutViewModelAsync(userId.Value);
        if (!cartVm.Items.Any())
        {
            TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống.";
            return RedirectToAction("Index", "Cart");
        }

        cartVm.ReceiverName = User.Identity?.Name ?? string.Empty;
        cartVm.Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        cartVm.PaymentMethod = PaymentMethods.CashOnDelivery;

        return View(cartVm);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel model)
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Challenge();
        }

        var cartItems = await _context.CartItems
            .Include(x => x.Product)
            .Where(x => x.UserId == userId.Value)
            .ToListAsync();

        model.Items = cartItems.Select(x => new CartLineViewModel
        {
            CartItemId = x.Id,
            ProductId = x.ProductId,
            ProductName = x.Product?.Name ?? "Sản phẩm đã xóa",
            ProductImage = ImageHelper.GetFirstImageOrDefault(x.Product?.Images),
            UnitPrice = x.UnitPrice,
            Quantity = x.Quantity,
            MaxStock = x.Product?.Stock ?? 0
        }).ToList();

        if (!cartItems.Any())
        {
            TempData["ErrorMessage"] = "Giỏ hàng của bạn đang trống.";
            return RedirectToAction("Index", "Cart");
        }

        if (!PaymentMethods.All.Contains(model.PaymentMethod))
        {
            ModelState.AddModelError(nameof(model.PaymentMethod), "Phương thức thanh toán không hợp lệ.");
        }

        foreach (var item in cartItems)
        {
            if (item.Product == null)
            {
                ModelState.AddModelError(string.Empty, "Có sản phẩm không còn tồn tại trong hệ thống.");
                continue;
            }

            if (item.Quantity > item.Product.Stock)
            {
                ModelState.AddModelError(string.Empty, $"Sản phẩm \"{item.Product.Name}\" chỉ còn {item.Product.Stock} sản phẩm trong kho.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var order = new Order
            {
                UserId = userId.Value,
                OrderCode = await GenerateOrderCodeAsync(),
                ReceiverName = model.ReceiverName.Trim(),
                Email = model.Email.Trim().ToLowerInvariant(),
                Phone = model.Phone.Trim(),
                ShippingAddress = model.ShippingAddress.Trim(),
                Note = string.IsNullOrWhiteSpace(model.Note) ? null : model.Note.Trim(),
                PaymentMethod = model.PaymentMethod,
                PaymentStatus = PaymentStatuses.Pending,
                OrderStatus = OrderStatuses.Pending,
                TotalAmount = cartItems.Sum(x => x.UnitPrice * x.Quantity),
                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            foreach (var cartItem in cartItems)
            {
                var product = cartItem.Product!;
                product.Stock -= cartItem.Quantity;

                _context.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = cartItem.ProductId,
                    ProductName = product.Name,
                    UnitPrice = cartItem.UnitPrice,
                    Quantity = cartItem.Quantity,
                    Subtotal = cartItem.UnitPrice * cartItem.Quantity
                });

                _context.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = product.Id,
                    QuantityChanged = -cartItem.Quantity,
                    QuantityAfter = product.Stock,
                    Type = "CHECKOUT",
                    Note = $"Xuất kho do tạo đơn {order.OrderCode}",
                    ReferenceCode = order.OrderCode,
                    CreatedAt = DateTime.UtcNow
                });
            }

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = $"Đặt hàng thành công. Mã đơn của bạn là {order.OrderCode}.";
            return RedirectToAction(nameof(Details), new { id = order.Id });
        }
        catch
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "Không thể tạo đơn hàng. Vui lòng thử lại.";
            return View(model);
        }
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var userId = User.GetUserId();
        if (userId == null)
        {
            return Challenge();
        }

        var orders = await _context.Orders
            .Include(x => x.Items)
            .Where(x => x.UserId == userId.Value)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var customOrders = await _context.CustomOrderRequests
            .Include(x => x.Product)
            .Where(x => x.UserId == userId.Value)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var viewModel = new OrderHistoryIndexViewModel
        {
            Items = orders.Select(order => new OrderHistoryItemViewModel
            {
                Id = order.Id,
                IsCustomOrder = false,
                Code = order.OrderCode,
                CreatedAt = order.CreatedAt,
                Title = "Đơn mua hàng",
                Description = $"{order.Items.Sum(x => x.Quantity)} sản phẩm • {order.TotalAmount:N0} VND",
                Quantity = order.Items.Sum(x => x.Quantity),
                TotalAmount = order.TotalAmount,
                StatusLabel = OrderUiHelper.OrderStatusLabel(order.OrderStatus),
                StatusBadgeClass = OrderUiHelper.OrderStatusBadgeClass(order.OrderStatus),
                PaymentStatusLabel = OrderUiHelper.PaymentStatusLabel(order.PaymentStatus),
                PaymentBadgeClass = OrderUiHelper.PaymentStatusBadgeClass(order.PaymentStatus)
            })
            .Concat(customOrders.Select(request => new OrderHistoryItemViewModel
            {
                Id = request.Id,
                IsCustomOrder = true,
                Code = request.RequestCode,
                CreatedAt = request.CreatedAt,
                Title = request.RequestedProductName,
                Description = $"Yêu cầu đặt riêng • Số lượng: {request.Quantity}" +
                              (request.EstimatedBudget.HasValue ? $" • Ngân sách: {request.EstimatedBudget.Value:N0} VND" : string.Empty),
                Quantity = request.Quantity,
                TotalAmount = request.EstimatedBudget,
                StatusLabel = CustomOrderUiHelper.StatusLabel(request.Status),
                StatusBadgeClass = CustomOrderUiHelper.StatusBadgeClass(request.Status)
            }))
            .OrderByDescending(x => x.CreatedAt)
            .ToList()
        };

        return View(viewModel);
    }

    [Authorize]
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.GetUserId();
        var order = await _context.Orders
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        if (!User.IsInRole("admin") && userId != order.UserId)
        {
            return Forbid();
        }

        return View(order);
    }

    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Admin()
    {
        var orders = await _context.Orders
            .Include(x => x.User)
            .Include(x => x.Items)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return View(orders);
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string orderStatus, string paymentStatus)
    {
        if (!OrderStatuses.All.Contains(orderStatus))
        {
            TempData["ErrorMessage"] = "Trạng thái đơn hàng không hợp lệ.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (!PaymentStatuses.All.Contains(paymentStatus))
        {
            TempData["ErrorMessage"] = "Trạng thái thanh toán không hợp lệ.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var order = await _context.Orders
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (order == null)
        {
            return NotFound();
        }

        if (order.OrderStatus == OrderStatuses.Cancelled && orderStatus != OrderStatuses.Cancelled)
        {
            TempData["ErrorMessage"] = "Đơn đã hủy không nên chuyển ngược sang trạng thái khác.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (order.OrderStatus != OrderStatuses.Cancelled && orderStatus == OrderStatuses.Cancelled)
            {
                foreach (var item in order.Items)
                {
                    if (item.Product == null)
                    {
                        continue;
                    }

                    item.Product.Stock += item.Quantity;
                    _context.InventoryTransactions.Add(new InventoryTransaction
                    {
                        ProductId = item.ProductId,
                        QuantityChanged = item.Quantity,
                        QuantityAfter = item.Product.Stock,
                        Type = "CANCEL_RETURN",
                        Note = $"Hoàn kho do hủy đơn {order.OrderCode}",
                        ReferenceCode = order.OrderCode,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            order.OrderStatus = orderStatus;
            order.PaymentStatus = paymentStatus;
            order.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] = "Đã cập nhật trạng thái đơn hàng.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch
        {
            await transaction.RollbackAsync();
            TempData["ErrorMessage"] = "Không thể cập nhật trạng thái đơn hàng.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Track(string? orderCode, string? email)
    {
        var vm = new TrackOrderViewModel
        {
            OrderCode = orderCode,
            Email = email,
            HasSearched = !string.IsNullOrWhiteSpace(orderCode) || !string.IsNullOrWhiteSpace(email)
        };

        if (!string.IsNullOrWhiteSpace(orderCode) && !string.IsNullOrWhiteSpace(email))
        {
            vm.Order = await _context.Orders
                .Include(x => x.Items)
                .FirstOrDefaultAsync(x =>
                    x.OrderCode == orderCode.Trim() &&
                    x.Email.ToLower() == email.Trim().ToLower());
        }

        return View(vm);
    }

    private async Task<CheckoutViewModel> BuildCheckoutViewModelAsync(int userId)
    {
        var cartItems = await _context.CartItems
            .Include(x => x.Product)
            .Where(x => x.UserId == userId)
            .ToListAsync();

        return new CheckoutViewModel
        {
            Items = cartItems.Select(x => new CartLineViewModel
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
    }

    private async Task<string> GenerateOrderCodeAsync()
    {
        while (true)
        {
            var code = $"DH{DateTime.Now:yyyyMMddHHmmss}{Random.Shared.Next(100, 999)}";
            var exists = await _context.Orders.AnyAsync(x => x.OrderCode == code);
            if (!exists)
            {
                return code;
            }
        }
    }
}
