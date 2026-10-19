using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

public class CatalogModel : PageModel
{
    private readonly PharmacyContext _context;
    public CatalogModel(PharmacyContext context) => _context = context;

    // Группируем продукты по категориям прямо в базе
    public Dictionary<string, List<Product>> GroupedProducts { get; set; }

    public async Task OnGetAsync()
    {
        var allProducts = await _context.Products.ToListAsync();
        GroupedProducts = allProducts
            .Where(p => p.Form != null)
            .GroupBy(p => p.Form)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
}