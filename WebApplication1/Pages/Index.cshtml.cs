using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;

namespace WebApplication1.Pages
{
    public class IndexModel : PageModel
    {
        private readonly PharmacyContext _context;
        public IndexModel(PharmacyContext context) => _context = context;

        public List<string> Categories { get; set; } = new();
        public string SearchDataJson { get; set; } = "[]";

        public async Task OnGetAsync()
        {
            Categories = await _context.Products
                .Select(p => p.Form)
                .Where(f => f != null)
                .Distinct()
                .ToListAsync();

            var products = await _context.Products
                .Select(p => new {
                    productId = p.ProductId,
                    name = p.Name,
                    active = p.ActiveSubstance
                })
                .ToListAsync();

            SearchDataJson = JsonSerializer.Serialize(products);
        }

        // НОВЫЙ МЕТОД: Обработка выхода прямо здесь
        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await HttpContext.SignOutAsync(); // Удаляем куки авторизации
            return RedirectToPage("/Index"); // Перенаправляем на чистый индекс
        }
    }
}