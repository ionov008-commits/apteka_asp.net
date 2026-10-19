using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApplication1.Models;

namespace WebApplication1.Pages.Account
{
    [Authorize(Roles = "owner")]
    public class ManagerModel : PageModel
    {
        private readonly PharmacyContext _context;
        public ManagerModel(PharmacyContext context) => _context = context;

        public WebApplication1.Models.Pharmacy CurrentPharmacy { get; set; }
        public List<StockViewModel> StockList { get; set; }
        public List<Product> AvailableProducts { get; set; } = new List<Product>();

        // Статистика
        public int TotalItemsCount { get; set; }
        public decimal TotalStockValue { get; set; }

        public class StockViewModel
        {
            public int StockId { get; set; }
            public Product Product { get; set; }
            public int Quantity { get; set; }
            public decimal Price { get; set; }
            public int ReservedQuantity { get; set; }
        }

        [BindProperty]
        public WebApplication1.Models.Pharmacy PharmacyUpdate { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToPage("/Account/Login");

            int uid = int.Parse(userIdStr);
            CurrentPharmacy = await _context.Pharmacies.FirstOrDefaultAsync(p => p.UserId == uid);

            if (CurrentPharmacy == null) return RedirectToPage("/Index");

            // Расчет брони по активным заказам (считаем только то, что еще не выдано)
            var reservations = await _context.OrderItems
                .Where(oi => oi.Stock.PharmacyId == CurrentPharmacy.PharmacyId
                          && oi.Status == "Pending")
                .GroupBy(oi => oi.StockId)
                .Select(g => new { StockId = g.Key, TotalReserved = g.Sum(x => x.Quantity) })
                .ToListAsync();

            var stocks = await _context.PharmacyStocks
                .Include(s => s.Product)
                .Where(s => s.PharmacyId == CurrentPharmacy.PharmacyId)
                .ToListAsync();

            StockList = stocks.Select(s => new StockViewModel
            {
                StockId = s.StockId,
                Product = s.Product,
                Quantity = s.Quantity ?? 0,
                Price = s.Price ?? 0,
                ReservedQuantity = reservations.FirstOrDefault(r => r.StockId == s.StockId)?.TotalReserved ?? 0
            }).ToList();

            TotalItemsCount = StockList.Sum(x => x.Quantity);
            TotalStockValue = StockList.Sum(x => x.Quantity * x.Price);

            // Получаем список препаратов, которых еще нет в этой аптеке, для добавления
            var existingStockProductIds = stocks.Select(s => s.ProductId).ToList();
            AvailableProducts = await _context.Products
                .Where(p => !existingStockProductIds.Contains(p.ProductId))
                .OrderBy(p => p.Name)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostUpdatePharmacyAsync()
        {
            var dbP = await _context.Pharmacies.FindAsync(PharmacyUpdate.PharmacyId);
            if (dbP != null)
            {
                dbP.Name = PharmacyUpdate.Name;
                dbP.Address = PharmacyUpdate.Address;
                dbP.Phone = PharmacyUpdate.Phone;
                dbP.WorkingHours = PharmacyUpdate.WorkingHours;
                dbP.DirectorName = PharmacyUpdate.DirectorName;
                dbP.Latitude = PharmacyUpdate.Latitude;
                dbP.Longitude = PharmacyUpdate.Longitude;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateStockAsync(int stockId, int quantity, decimal price)
        {
            var stock = await _context.PharmacyStocks.FindAsync(stockId);
            if (stock != null)
            {
                stock.Quantity = quantity;
                stock.Price = price;
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddStockAsync(int newProductId, int newQuantity, decimal newPrice)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return RedirectToPage("/Account/Login");

            int uid = int.Parse(userIdStr);
            var pharmacy = await _context.Pharmacies.FirstOrDefaultAsync(p => p.UserId == uid);

            if (pharmacy != null && newProductId > 0)
            {
                var newStock = new PharmacyStock
                {
                    PharmacyId = pharmacy.PharmacyId,
                    ProductId = newProductId,
                    Quantity = newQuantity,
                    Price = newPrice
                };
                _context.PharmacyStocks.Add(newStock);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await HttpContext.SignOutAsync();
            return RedirectToPage("/Index");
        }
    }
}