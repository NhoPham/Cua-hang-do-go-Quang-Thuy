using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QLBH.Models;

namespace QLBH.Controllers;

public class ProductController : Controller
{
    private readonly QlbhContext _context;

    public ProductController(QlbhContext context)
    {
        _context = context;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index(
        string? searchName,
        int? categoryId,
        decimal? minPrice,
        decimal? maxPrice,
        string? sortPrice,
        string? stockStatus,
        int page = 1)
    {
        const int pageSize = 8;

        if (page < 1)
        {
            page = 1;
        }

        if (minPrice.HasValue && maxPrice.HasValue && minPrice > maxPrice)
        {
            var temp = minPrice;
            minPrice = maxPrice;
            maxPrice = temp;
        }

        var products = _context.Products
            .Include(p => p.Category)
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchName))
        {
            var keyword = searchName.Trim().ToLower();
            products = products.Where(p => p.Name.ToLower().Contains(keyword));
        }

        if (categoryId.HasValue)
        {
            products = products.Where(p => p.CategoryId == categoryId.Value);
        }

        if (minPrice.HasValue)
        {
            products = products.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            products = products.Where(p => p.Price <= maxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(stockStatus))
        {
            if (stockStatus == "in")
            {
                products = products.Where(p => p.Stock > 0);
            }
            else if (stockStatus == "out")
            {
                products = products.Where(p => p.Stock <= 0);
            }
        }

        products = sortPrice switch
        {
            "price_asc" => products.OrderBy(p => p.Price).ThenByDescending(p => p.Id),
            "price_desc" => products.OrderByDescending(p => p.Price).ThenByDescending(p => p.Id),
            _ => products.OrderByDescending(p => p.Id)
        };

        var totalProducts = await products.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

        if (totalPages == 0)
        {
            totalPages = 1;
        }

        if (page > totalPages)
        {
            page = totalPages;
        }

        var productList = await products
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", categoryId);
        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.SearchName = searchName;
        ViewBag.SelectedCategoryId = categoryId;
        ViewBag.MinPrice = minPrice;
        ViewBag.MaxPrice = maxPrice;
        ViewBag.SortPrice = sortPrice;
        ViewBag.StockStatus = stockStatus;

        return View(productList);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    [Authorize(Roles = "admin")]
    public IActionResult Create()
    {
        ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name");
        ViewBag.NewCategoryName = string.Empty;
        ViewBag.NewCategoryDescription = string.Empty;
        return View();
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        Product product,
        List<IFormFile>? Images,
        List<IFormFile>? NewImages,
        string? NewCategoryName,
        string? NewCategoryDescription)
    {
        NewCategoryName = NewCategoryName?.Trim();
        NewCategoryDescription = NewCategoryDescription?.Trim();

        if (!string.IsNullOrWhiteSpace(NewCategoryName))
        {
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == NewCategoryName.ToLower());

            if (existingCategory == null)
            {
                var newCategory = new Category
                {
                    Name = NewCategoryName,
                    Description = string.IsNullOrWhiteSpace(NewCategoryDescription) ? null : NewCategoryDescription
                };

                _context.Categories.Add(newCategory);
                await _context.SaveChangesAsync();

                product.CategoryId = newCategory.Id;
            }
            else
            {
                product.CategoryId = existingCategory.Id;
            }
        }

        if (!product.CategoryId.HasValue)
        {
            ModelState.AddModelError("CategoryId", "Vui lòng chọn danh mục hoặc nhập danh mục mới.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            ViewBag.NewCategoryName = NewCategoryName;
            ViewBag.NewCategoryDescription = NewCategoryDescription;
            return View(product);
        }

        var uploadedFiles = PickUploadedFiles(Images, NewImages);
        var imagePaths = await SaveUploadedImagesAsync(uploadedFiles);

        product.Images = imagePaths.Any() ? JsonConvert.SerializeObject(imagePaths) : "[]";
        product.IsDeleted = false;

        _context.Add(product);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Tạo sản phẩm thành công.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (product == null)
        {
            return NotFound();
        }

        ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
        return View(product);
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        Product product,
        List<IFormFile>? Images,
        List<IFormFile>? NewImages)
    {
        if (id != product.Id)
        {
            return NotFound();
        }

        var existingProduct = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (existingProduct == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            ViewData["CategoryId"] = new SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        try
        {
            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.Price = product.Price;
            existingProduct.Stock = product.Stock;
            existingProduct.CategoryId = product.CategoryId;

            var uploadedFiles = PickUploadedFiles(Images, NewImages);

            if (uploadedFiles.Any())
            {
                DeleteImagesFromDisk(existingProduct.Images);

                var newImagePaths = await SaveUploadedImagesAsync(uploadedFiles);
                existingProduct.Images = newImagePaths.Any() ? JsonConvert.SerializeObject(newImagePaths) : "[]";
            }
            else if (string.IsNullOrWhiteSpace(existingProduct.Images))
            {
                existingProduct.Images = string.IsNullOrWhiteSpace(product.Images) ? "[]" : product.Images;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Cập nhật sản phẩm thành công.";
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ProductExists(product.Id))
            {
                return NotFound();
            }

            throw;
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var product = await _context.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);

        if (product == null)
        {
            return NotFound();
        }

        var hasOrderItem = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id.Value);
        ViewBag.HasOrderItem = hasOrderItem;

        return View(product);
    }

    [Authorize(Roles = "admin")]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy sản phẩm cần xóa.";
            return RedirectToAction(nameof(Index));
        }

        if (product.IsDeleted)
        {
            TempData["ErrorMessage"] = "Sản phẩm này đã được xóa trước đó.";
            return RedirectToAction(nameof(Index));
        }

        var hasOrderItem = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);

        if (hasOrderItem)
        {
            product.IsDeleted = true;
            product.Stock = 0;

            var cartItems = await _context.CartItems
                .Where(c => c.ProductId == id)
                .ToListAsync();

            if (cartItems.Any())
            {
                _context.CartItems.RemoveRange(cartItems);
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Sản phẩm đã được ẩn khỏi cửa hàng vì đã phát sinh đơn hàng. Lịch sử đơn hàng vẫn được giữ lại.";
            return RedirectToAction(nameof(Index));
        }

        var imagesJson = product.Images;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();

        DeleteImagesFromDisk(imagesJson);

        TempData["SuccessMessage"] = "Xóa sản phẩm thành công.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAll()
    {
        var products = await _context.Products
            .Where(p => !p.IsDeleted)
            .ToListAsync();

        if (!products.Any())
        {
            TempData["ErrorMessage"] = "Không có sản phẩm nào để xóa.";
            return RedirectToAction(nameof(Index));
        }

        var orderedProductIds = await _context.OrderItems
            .Select(oi => oi.ProductId)
            .Distinct()
            .ToListAsync();

        var productsToHide = products
            .Where(p => orderedProductIds.Contains(p.Id))
            .ToList();

        var productsToDelete = products
            .Where(p => !orderedProductIds.Contains(p.Id))
            .ToList();

        var affectedProductIds = products.Select(p => p.Id).ToList();

        var cartItems = await _context.CartItems
            .Where(c => affectedProductIds.Contains(c.ProductId))
            .ToListAsync();

        if (cartItems.Any())
        {
            _context.CartItems.RemoveRange(cartItems);
        }

        foreach (var product in productsToHide)
        {
            product.IsDeleted = true;
            product.Stock = 0;
        }

        var imageJsonList = productsToDelete
            .Select(p => p.Images)
            .ToList();

        if (productsToDelete.Any())
        {
            _context.Products.RemoveRange(productsToDelete);
        }

        await _context.SaveChangesAsync();

        foreach (var imagesJson in imageJsonList)
        {
            DeleteImagesFromDisk(imagesJson);
        }

        if (!await _context.Products.AnyAsync())
        {
            await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Product', RESEED, 0)");
        }

        TempData["SuccessMessage"] = $"Đã xử lý xóa sản phẩm. Xóa hẳn {productsToDelete.Count} sản phẩm chưa phát sinh đơn hàng, ẩn {productsToHide.Count} sản phẩm đã có đơn hàng.";

        return RedirectToAction(nameof(Index));
    }

    private bool ProductExists(int id)
    {
        return _context.Products.Any(e => e.Id == id);
    }

    private static List<IFormFile> PickUploadedFiles(List<IFormFile>? images, List<IFormFile>? newImages)
    {
        if (newImages != null && newImages.Any(f => f.Length > 0))
        {
            return newImages.Where(f => f.Length > 0).ToList();
        }

        if (images != null && images.Any(f => f.Length > 0))
        {
            return images.Where(f => f.Length > 0).ToList();
        }

        return new List<IFormFile>();
    }

    private async Task<List<string>> SaveUploadedImagesAsync(List<IFormFile> files)
    {
        var imagePaths = new List<string>();

        if (files == null || !files.Any())
        {
            return imagePaths;
        }

        var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products");

        if (!Directory.Exists(uploadPath))
        {
            Directory.CreateDirectory(uploadPath);
        }

        foreach (var file in files)
        {
            if (file.Length <= 0)
            {
                continue;
            }

            var extension = Path.GetExtension(file.FileName);
            var fileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadPath, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            imagePaths.Add($"/images/products/{fileName}");
        }

        return imagePaths;
    }

    private void DeleteImagesFromDisk(string? imagesJson)
    {
        if (string.IsNullOrWhiteSpace(imagesJson))
        {
            return;
        }

        List<string> imageList;

        try
        {
            imageList = JsonConvert.DeserializeObject<List<string>>(imagesJson) ?? new List<string>();
        }
        catch
        {
            imageList = new List<string> { imagesJson };
        }

        foreach (var imagePath in imageList)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
            {
                continue;
            }

            if (!imagePath.StartsWith("/images/products/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var fullPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                imagePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }
}