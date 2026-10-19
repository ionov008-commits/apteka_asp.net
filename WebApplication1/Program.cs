using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using WebApplication1.Services;

var builder = WebApplication.CreateBuilder(args);

// ---  ”À‹“”–¿ ---
var cultureInfo = System.Globalization.CultureInfo.InvariantCulture;
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// --- —≈–¬»—€ ---
builder.Services.AddHostedService<OrderExpirationWorker>();
builder.Services.AddTransient<IQrCodeService, QrCodeService>();
builder.Services.AddDbContext<PharmacyContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// 1. ƒŒ¡¿¬Àﬂ≈Ã œŒƒƒ≈–∆ ” —≈——»… (ƒÀﬂ  ¿œ◊»)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10); //  ‡Ô˜‡ ÊË‚ÂÚ 10 ÏËÌÛÚ
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
    });

builder.Services.AddRazorPages();
builder.Services.AddHttpClient();
builder.Services.AddScoped<CartManager>();

var app = builder.Build();

// ---  ŒÕ¬≈…≈– (MIDDLEWARE) ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 2. ¬ Àﬁ◊¿≈Ã —≈——»» (—ÚÓ„Ó œŒ—À≈ UseRouting, ÌÓ œ≈–≈ƒ Authentication)
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.Run();