using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace WebApplication1.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private readonly PharmacyContext _context;
        private readonly IWebHostEnvironment _environment;

        public DashboardModel(PharmacyContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public List<User> Users { get; set; }
        public List<WebApplication1.Models.Pharmacy> Pharmacies { get; set; }
        public List<Product> Products { get; set; }

        [BindProperty]
        public Product NewProduct { get; set; }

        [BindProperty]
        public bool IsPrescriptionRequired { get; set; }

        [BindProperty]
        public IFormFile? ProductImage { get; set; } // Для загрузки фото

        public async Task OnGetAsync()
        {
            Users = await _context.Users.ToListAsync();
            Pharmacies = await _context.Pharmacies.ToListAsync();
            Products = await _context.Products.ToListAsync();
        }

        public async Task<IActionResult> OnPostAddProductAsync()
        {
            NewProduct.PrescriptionRequired = IsPrescriptionRequired ? 1 : 0;
            ModelState.Remove("NewProduct.PrescriptionRequired");

            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            // 1. Сначала сохраняем, чтобы получить ID
            _context.Products.Add(NewProduct);
            await _context.SaveChangesAsync();

            if (ProductImage != null)
            {
                string folderPath = Path.Combine(_environment.WebRootPath, "images", "products");
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

                string fileExtension = Path.GetExtension(ProductImage.FileName);
                // Имя файла: "15.jpg"
                string fileName = NewProduct.ProductId.ToString() + fileExtension;
                string filePath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ProductImage.CopyToAsync(stream);
                }

                // ВАЖНО: Сохраняем ТОЛЬКО имя файла, т.к. ProductCard сам добавит путь
                NewProduct.Image = fileName;
                await _context.SaveChangesAsync();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
            }
            return RedirectToPage();
        }
    }
}