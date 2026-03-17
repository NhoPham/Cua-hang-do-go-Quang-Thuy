using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLBH.Models;
using QLBH.Services;
using QLBH.Utils;
using QLBH.ViewModels;

namespace QLBH.Controllers;

public class AccountController : Controller
{
    private readonly QlbhContext _context;
    private readonly IEmailService _emailService;

    public AccountController(QlbhContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var keyword = model.UsernameOrEmail.Trim().ToLower();

        var account = await _context.Users.FirstOrDefaultAsync(x =>
            x.Username.ToLower() == keyword || x.Email.ToLower() == keyword);

        if (account == null)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không đúng.");
            return View(model);
        }

        var validPassword = PasswordHelper.VerifyPassword(model.Password, account.Password)
                            || account.Password == model.Password;

        if (!validPassword)
        {
            ModelState.AddModelError(string.Empty, "Tài khoản hoặc mật khẩu không đúng.");
            return View(model);
        }

        if (account.Password == model.Password)
        {
            account.Password = PasswordHelper.HashPassword(model.Password);
            await _context.SaveChangesAsync();
        }

        await SignInUserAsync(account, model.RememberMe);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        return View();
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var username = model.Username.Trim();
        var email = model.Email.Trim().ToLower();

        if (await _context.Users.AnyAsync(x => x.Username.ToLower() == username.ToLower()))
        {
            ModelState.AddModelError(nameof(model.Username), "Tên đăng nhập đã tồn tại.");
            return View(model);
        }

        if (await _context.Users.AnyAsync(x => x.Email.ToLower() == email))
        {
            ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng.");
            return View(model);
        }

        var account = new User
        {
            Username = username,
            Email = email,
            Password = PasswordHelper.HashPassword(model.Password),
            Role = "customer"
        };

        _context.Users.Add(account);
        await _context.SaveChangesAsync();

        await SignInUserAsync(account, false);

        TempData["SuccessMessage"] = "Đăng ký thành công.";
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim().ToLower();
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == email);

        ViewBag.InfoMessage = "Nếu email tồn tại trong hệ thống, liên kết khôi phục đã được tạo.";

        if (user == null)
        {
            return View(new ForgotPasswordViewModel());
        }

        var activeTokens = await _context.PasswordResetTokens
            .Where(x => x.UserId == user.Id && x.UsedAt == null)
            .ToListAsync();

        foreach (var item in activeTokens)
        {
            item.UsedAt = DateTime.UtcNow;
        }

        var tokenValue = $"{Guid.NewGuid():N}{Guid.NewGuid():N}";

        _context.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId = user.Id,
            Token = tokenValue,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        });

        await _context.SaveChangesAsync();

        var resetLink = Url.Action(
            nameof(ResetPassword),
            "Account",
            new { token = tokenValue },
            Request.Scheme) ?? string.Empty;

        var emailBody = $"""
            <p>Xin chào <strong>{user.Username}</strong>,</p>
            <p>Bạn vừa yêu cầu đặt lại mật khẩu cho tài khoản tại website Đồ Gỗ Quảng Thủy.</p>
            <p>Bấm vào liên kết dưới đây để đặt lại mật khẩu (hiệu lực 1 giờ):</p>
            <p><a href="{resetLink}">{resetLink}</a></p>
            """;

        var sent = await _emailService.SendAsync(user.Email, "Khôi phục mật khẩu - Đồ Gỗ Quảng Thủy", emailBody);

        if (!sent)
        {
            ViewBag.DebugResetLink = resetLink;
            ViewBag.InfoMessage = "SMTP chưa cấu hình hoặc gửi mail thất bại. Bạn có thể dùng link debug để test local.";
        }

        return View(new ForgotPasswordViewModel());
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> ResetPassword(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            TempData["ErrorMessage"] = "Liên kết đặt lại mật khẩu không hợp lệ.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        var resetToken = await _context.PasswordResetTokens
            .FirstOrDefaultAsync(x => x.Token == token && x.UsedAt == null && x.ExpiresAt > DateTime.UtcNow);

        if (resetToken == null)
        {
            TempData["ErrorMessage"] = "Liên kết đặt lại mật khẩu đã hết hạn hoặc không tồn tại.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        return View(new ResetPasswordViewModel { Token = token });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var resetToken = await _context.PasswordResetTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == model.Token && x.UsedAt == null && x.ExpiresAt > DateTime.UtcNow);

        if (resetToken == null)
        {
            TempData["ErrorMessage"] = "Liên kết đặt lại mật khẩu đã hết hạn hoặc không tồn tại.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        resetToken.User.Password = PasswordHelper.HashPassword(model.NewPassword);
        resetToken.UsedAt = DateTime.UtcNow;

        var otherTokens = await _context.PasswordResetTokens
            .Where(x => x.UserId == resetToken.UserId && x.Id != resetToken.Id && x.UsedAt == null)
            .ToListAsync();

        foreach (var item in otherTokens)
        {
            item.UsedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập bằng mật khẩu mới.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task SignInUserAsync(User account, bool isPersistent)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Name, account.Username),
            new(ClaimTypes.Email, account.Email),
            new(ClaimTypes.Role, account.Role ?? "customer")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = isPersistent ? DateTimeOffset.UtcNow.AddDays(7) : null
            });
    }
}
