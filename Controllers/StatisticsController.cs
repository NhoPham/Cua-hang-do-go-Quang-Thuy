using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLBH.Models;
using QLBH.Utils;
using QLBH.ViewModels;

namespace QLBH.Controllers;

[Authorize(Roles = "admin")]
public class StatisticsController : Controller
{
    private readonly QlbhContext _context;

    public StatisticsController(QlbhContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        var fromDate = (from ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
        var toDate = (to ?? DateTime.Today).Date.AddDays(1).AddTicks(-1);

        var orders = await _context.Orders
            .Include(x => x.Items)
            .Where(x => x.CreatedAt >= fromDate && x.CreatedAt <= toDate)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var validOrders = orders.Where(x => x.OrderStatus != OrderStatuses.Cancelled).ToList();

        var topProducts = validOrders
            .SelectMany(x => x.Items)
            .GroupBy(x => x.ProductName)
            .Select(g => new StatisticsTopProductRow
            {
                ProductName = g.Key,
                QuantitySold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.Subtotal)
            })
            .OrderByDescending(x => x.QuantitySold)
            .ThenByDescending(x => x.Revenue)
            .Take(10)
            .ToList();

        var revenueByDay = validOrders
            .GroupBy(x => x.CreatedAt.Date)
            .Select(g => new StatisticsDailyRevenueRow
            {
                Date = g.Key,
                Orders = g.Count(),
                Revenue = g.Sum(x => x.TotalAmount)
            })
            .OrderBy(x => x.Date)
            .ToList();

        var statusBreakdown = orders
            .GroupBy(x => x.OrderStatus)
            .Select(g => new StatisticsStatusRow
            {
                Status = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        var lowStock = await _context.Products
            .Where(x => x.Stock <= 5)
            .OrderBy(x => x.Stock)
            .ThenBy(x => x.Name)
            .Select(x => new StatisticsLowStockRow
            {
                ProductId = x.Id,
                ProductName = x.Name,
                Stock = x.Stock
            })
            .Take(10)
            .ToListAsync();

        var vm = new StatisticsDashboardViewModel
        {
            From = fromDate,
            To = toDate,
            TotalOrders = orders.Count,
            PendingOrders = orders.Count(x => x.OrderStatus == OrderStatuses.Pending),
            CompletedOrders = orders.Count(x => x.OrderStatus == OrderStatuses.Completed),
            TotalUnitsSold = validOrders.SelectMany(x => x.Items).Sum(x => x.Quantity),
            TotalRevenue = validOrders.Sum(x => x.TotalAmount),
            TopProducts = topProducts,
            RevenueByDay = revenueByDay,
            StatusBreakdown = statusBreakdown,
            LowStockProducts = lowStock
        };

        return View(vm);
    }
}
