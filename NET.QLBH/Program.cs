using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using QLBH.Models;
using QLBH.Services;
using QLBH.Settings;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<QlbhContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("SmtpSettings"));

builder.Services.Configure<ChatAssistantSettings>(builder.Configuration.GetSection("ChatAssistant"));
builder.Services.PostConfigure<ChatAssistantSettings>(options =>
{
    if (string.IsNullOrWhiteSpace(options.ApiKey))
    {
        options.ApiKey = builder.Configuration["OPENAI_API_KEY"] ?? string.Empty;
    }

    if (string.IsNullOrWhiteSpace(options.Model))
    {
        options.Model = "llama3.2";
    }

    if (string.IsNullOrWhiteSpace(options.Endpoint))
    {
        options.Endpoint = "http://localhost:11434/api/generate";
    }

    if (string.IsNullOrWhiteSpace(options.SystemPrompt))
    {
        options.SystemPrompt =
            "Bạn là trợ lý tư vấn bán hàng cho cửa hàng Đồ Gỗ Quảng Thủy. " +
            "Luôn trả lời bằng tiếng Việt, lịch sự, ngắn gọn, thực tế và dễ hiểu.";
    }
});

builder.Services.PostConfigure<ChatAssistantSettings>(options =>
{
    var legacySection = builder.Configuration.GetSection("AIChat");
    if (!legacySection.Exists())
    {
        return;
    }

    options.ApiKey = string.IsNullOrWhiteSpace(options.ApiKey)
        ? legacySection["ApiKey"] ?? options.ApiKey
        : options.ApiKey;

    options.Model = string.IsNullOrWhiteSpace(options.Model)
        ? legacySection["Model"] ?? options.Model
        : options.Model;

    options.SystemPrompt = string.IsNullOrWhiteSpace(options.SystemPrompt)
        ? legacySection["SystemPrompt"] ?? options.SystemPrompt
        : options.SystemPrompt;

    if (string.IsNullOrWhiteSpace(options.Endpoint))
    {
        options.Endpoint = legacySection["Endpoint"] ?? options.Endpoint;
    }
});

builder.Services.AddHttpClient<IChatAssistantService, ChatAssistantService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(45);
});

builder.Services.AddScoped<IEmailService, SmtpEmailService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "QLBH.Auth";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();