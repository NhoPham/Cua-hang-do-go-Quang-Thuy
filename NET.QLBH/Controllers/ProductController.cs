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
            .FirstOrDefaultAsync(m => m.Id == id);

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

        // Ưu tiên danh mục mới nếu người dùng có nhập
        if (!string.IsNullOrWhiteSpace(NewCategoryName))
        {
            var existingCategory = await _context.Categories
                .FirstOrDefaultAsync(c => c.Name.ToLower() == NewCategoryName.ToLower());

            if (existingCategory == null)
            {
                var newCategory = new Category
                {
                    Name = NewCategoryName,
                    Description = string.IsNullOrWhiteSpace(NewCategoryDescription)
                        ? null
                        : NewCategoryDescription
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

        product.Images = imagePaths.Any()
            ? JsonConvert.SerializeObject(imagePaths)
            : "[]";

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

        var product = await _context.Products.FindAsync(id);
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
    public async Task<IActionResult> Edit(int id, Product product, List<IFormFile>? Images, List<IFormFile>? NewImages)
    {
        if (id != product.Id)
        {
            return NotFound();
        }

        var existingProduct = await _context.Products.FindAsync(id);
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
                existingProduct.Images = newImagePaths.Any()
                    ? JsonConvert.SerializeObject(newImagePaths)
                    : "[]";
            }
            else if (string.IsNullOrWhiteSpace(existingProduct.Images))
            {
                existingProduct.Images = string.IsNullOrWhiteSpace(product.Images)
                    ? "[]"
                    : product.Images;
            }

            await _context.SaveChangesAsync();
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
            .FirstOrDefaultAsync(m => m.Id == id);

        if (product == null)
        {
            return NotFound();
        }

        return View(product);
    }

    [Authorize(Roles = "admin")]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var product = await _context.Products.FindAsync(id);

        if (product != null)
        {
            DeleteImagesFromDisk(product.Images);
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAll()
    {
        var allProducts = await _context.Products.ToListAsync();

        foreach (var product in allProducts)
        {
            DeleteImagesFromDisk(product.Images);
        }

        _context.Products.RemoveRange(allProducts);
        await _context.SaveChangesAsync();

        await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Product', RESEED, 0)");

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LoadAll()
    {
        var requiredCategories = new List<Category>
        {
            new() { Name = "Bàn", Description = "Các loại bàn gỗ" },
            new() { Name = "Ghế", Description = "Các loại ghế gỗ" },
            new() { Name = "Tủ", Description = "Các loại tủ gỗ" },
            new() { Name = "Giường", Description = "Các loại giường gỗ" },
            new() { Name = "Kệ", Description = "Các loại kệ gỗ" },
            new() { Name = "Bộ Bàn Ghế", Description = "Các loại bộ bàn ghế" }
        };

        foreach (var category in requiredCategories)
        {
            var exists = await _context.Categories.AnyAsync(c => c.Name == category.Name);
            if (!exists)
            {
                _context.Categories.Add(category);
            }
        }

        await _context.SaveChangesAsync();

        if (await _context.Products.AnyAsync())
        {
            return RedirectToAction(nameof(Index));
        }

        var categories = await _context.Categories.ToListAsync();

        var banId = categories.First(c => c.Name == "Bàn").Id;
        var gheId = categories.First(c => c.Name == "Ghế").Id;
        var tuId = categories.First(c => c.Name == "Tủ").Id;
        var giuongId = categories.First(c => c.Name == "Giường").Id;
        var keId = categories.First(c => c.Name == "Kệ").Id;

        var defaultProducts = new List<Product>
        {
            new()
            {
                Name = "Bàn ăn gỗ sồi 6 ghế",
                Description = "Bàn ăn gỗ sồi tự nhiên, thiết kế hiện đại, phù hợp gia đình.",
                Price = 8500000,
                Images = "[\"/images/default/ban-an-go-soi.jpg\"]",
                CategoryId = banId,
                Stock = 5
            },
            new()
            {
                Name = "Bàn làm việc gỗ MDF",
                Description = "Bàn làm việc gọn gàng, phù hợp phòng ngủ hoặc văn phòng nhỏ.",
                Price = 3200000,
                Images = "[\"/images/default/ban-lam-viec-mdf.jpg\"]",
                CategoryId = banId,
                Stock = 8
            },
            new()
            {
                Name = "Ghế gỗ óc chó",
                Description = "Ghế gỗ óc chó sang trọng, bền đẹp theo thời gian.",
                Price = 2500000,
                Images = "[\"/images/default/ghe-go-oc-cho.jpg\"]",
                CategoryId = gheId,
                Stock = 10
            },
            new()
            {
                Name = "Ghế ăn gỗ cao su",
                Description = "Ghế ăn chắc chắn, kiểu dáng đơn giản, phù hợp nhiều không gian.",
                Price = 950000,
                Images = "[\"/images/default/ghe-an-go-cao-su.jpg\"]",
                CategoryId = gheId,
                Stock = 20
            },
            new()
            {
                Name = "Tủ quần áo 3 cánh gỗ MDF",
                Description = "Tủ quần áo rộng rãi, thiết kế tiện dụng cho phòng ngủ gia đình.",
                Price = 7200000,
                Images = "[\"/images/default/tu-quan-ao-3-canh.jpg\"]",
                CategoryId = tuId,
                Stock = 4
            },
            new()
            {
                Name = "Tủ bếp gỗ công nghiệp",
                Description = "Tủ bếp hiện đại, chống ẩm tốt, dễ vệ sinh.",
                Price = 12500000,
                Images = "[\"/images/default/tu-bep-go-cong-nghiep.jpg\"]",
                CategoryId = tuId,
                Stock = 3
            },
            new()
            {
                Name = "Giường ngủ gỗ xoan đào 1m8",
                Description = "Giường ngủ chắc chắn, màu sắc ấm áp, phù hợp phòng ngủ chính.",
                Price = 9800000,
                Images = "[\"/images/default/giuong-go-xoan-dao.jpg\"]",
                CategoryId = giuongId,
                Stock = 3
            },
            new()
            {
                Name = "Giường tầng trẻ em gỗ tự nhiên",
                Description = "Giường tầng tiết kiệm không gian, an toàn cho trẻ nhỏ.",
                Price = 11500000,
                Images = "[\"/images/default/giuong-tang-tre-em.jpg\"]",
                CategoryId = giuongId,
                Stock = 2
            },
            new()
            {
                Name = "Kệ tivi gỗ hiện đại",
                Description = "Kệ tivi thiết kế tối giản, phù hợp phòng khách hiện đại.",
                Price = 4200000,
                Images = "[\"/images/default/ke-tivi-go.jpg\"]",
                CategoryId = keId,
                Stock = 6
            },
            new()
            {
                Name = "Kệ sách gỗ 5 tầng",
                Description = "Kệ sách nhiều ngăn, tiện lợi cho phòng làm việc hoặc phòng khách.",
                Price = 2800000,
                Images = "[\"/images/default/ke-sach-5-tang.jpg\"]",
                CategoryId = keId,
                Stock = 7
            }
        };

        _context.Products.AddRange(defaultProducts);
        await _context.SaveChangesAsync();

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

            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
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