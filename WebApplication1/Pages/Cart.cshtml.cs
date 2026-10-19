using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using System.Security.Claims;
using WebApplication1.Services;

namespace WebApplication1.Pages
{
    public class CartModel : PageModel
    {
        private readonly PharmacyContext _db;
        private readonly IQrCodeService _qrCodeService;
        private readonly IWebHostEnvironment _env;

        public CartModel(PharmacyContext db, IQrCodeService qrCodeService, IWebHostEnvironment env)
        {
            _db = db;
            _qrCodeService = qrCodeService;
            _env = env;
        }

        public CustomerOrder? Cart { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return RedirectToPage("/Account/Login");
            if (!User.IsInRole("customer")) return RedirectToPage("/Index");

            int userId = int.Parse(userIdClaim);

            Cart = await _db.CustomerOrders
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Stock)
                        .ThenInclude(s => s.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Stock)
                        .ThenInclude(s => s.Pharmacy)
                // КРИТИЧНО: Загружаем все OrderItems этого склада, чтобы AvailableQuantity сработал правильно
                .Include(o => o.OrderItems)
                    .ThenInclude(i => i.Stock)
                        .ThenInclude(s => s.OrderItems)
                            .ThenInclude(oi => oi.Order)
                .FirstOrDefaultAsync(o => o.Status == "cart" && o.Customer.UserId == userId);

            return Page();
        }

        public async Task<IActionResult> OnPostUpdateQuantityAsync(int stockId, int delta)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return RedirectToPage("/Account/Login");

            int userId = int.Parse(userIdClaim);

            var cart = await _db.CustomerOrders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Stock)
                        .ThenInclude(s => s.OrderItems)
                            .ThenInclude(oi => oi.Order)
                .FirstOrDefaultAsync(o => o.Status == "cart" && o.Customer.UserId == userId);

            if (cart == null) return RedirectToPage();

            var item = cart.OrderItems.FirstOrDefault(i => i.StockId == stockId);
            if (item != null)
            {
                // Проверка доступности: не даем превысить AvailableQuantity при увеличении
                if (delta > 0 && (item.Quantity + delta) > item.Stock.AvailableQuantity)
                {
                    item.Quantity = item.Stock.AvailableQuantity;
                }
                else
                {
                    item.Quantity += delta;
                }

                if (item.Quantity <= 0)
                {
                    _db.OrderItems.Remove(item);
                }
            }

            await _db.SaveChangesAsync();

            // Пересчитываем общую сумму корзины
            var remainingItems = await _db.OrderItems
                .Where(i => i.OrderId == cart.OrderId)
                .Include(i => i.Stock)
                .ToListAsync();

            cart.TotalCost = remainingItems.Sum(i => (decimal)i.Quantity * (i.Stock.Price ?? 0));
            _db.CustomerOrders.Update(cart);
            await _db.SaveChangesAsync();

            return RedirectToPage();
        }

        public async Task<JsonResult> OnGetCartCount()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim))
                    return new JsonResult(new { count = 0 });

                int userId = int.Parse(userIdClaim);

                // Считаем сумму Quantity всех элементов в заказе со статусом "cart"
                // Используем (int?), чтобы SumAsync не упал на пустой корзине
                var count = await _db.OrderItems
                    .Where(i => i.Order.Customer.UserId == userId && i.Order.Status == "cart")
                    .SumAsync(i => (int?)i.Quantity) ?? 0;

                return new JsonResult(new { count });
            }
            catch
            {
                return new JsonResult(new { count = 0 });
            }
        }

        public async Task<IActionResult> OnPostConfirmOrderAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return RedirectToPage("/Account/Login");

            int userId = int.Parse(userIdClaim);

            var cart = await _db.CustomerOrders
                .FirstOrDefaultAsync(o => o.Status == "cart" && o.Customer.UserId == userId);

            if (cart != null)
            {
                cart.Status = "active";
                cart.CreatedAt = DateTime.Now;
                cart.ExpiresAt = DateTime.Now.AddDays(7);
                await _db.SaveChangesAsync();

                string qrContent = $"PHARMACY_ORDER_{cart.OrderId}";
                byte[] qrBytes = _qrCodeService.GenerateQrCodeBytes(qrContent);

                string folderPath = Path.Combine(_env.WebRootPath, "images", "orders");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                string filePath = Path.Combine(folderPath, $"{cart.OrderId}.png");
                await System.IO.File.WriteAllBytesAsync(filePath, qrBytes);
            }

            return RedirectToPage("Profile");
        }
    }
}