using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebApplication1.Models;

namespace WebApplication1.Pages.Account
{
    [Authorize]
    public class ProfileModel : PageModel
    {
        private readonly PharmacyContext _context;

        public ProfileModel(PharmacyContext context)
        {
            _context = context;
        }

        public User? CurrentUser { get; set; }
        public List<OrderDisplayViewModel> Orders { get; set; } = new();

        [BindProperty]
        public EditProfileInput Input { get; set; } = new();

        public class EditProfileInput
        {
            public string Username { get; set; } = "";
            public string Email { get; set; } = "";
            public string FullName { get; set; } = "";
            public string Phone { get; set; } = "";
            public string Address { get; set; } = "";
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string? NewPassword { get; set; }

            [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Введите текущий пароль для подтверждения изменений")]
            public string CurrentPassword { get; set; } = "";
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var userId = GetUserId();
            if (userId == -1) return RedirectToPage("/Account/Login");

            await LoadUserData(userId);
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            var userId = GetUserId();
            if (userId == -1) return RedirectToPage("/Account/Login");

            var user = await _context.Users
                .Include(u => u.Customer)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return NotFound();

            // 1. Проверка текущего пароля
            if (!BCrypt.Net.BCrypt.Verify(Input.CurrentPassword, user.Password))
            {
                ModelState.AddModelError("Input.CurrentPassword", "Текущий пароль введен неверно");
            }

            // 2. Проверка уникальности логина
            if (user.Username != Input.Username && await _context.Users.AnyAsync(u => u.Username == Input.Username))
            {
                ModelState.AddModelError("Input.Username", "Этот логин уже занят.");
            }

            // 3. БЛОКИРОВКА ПРИ ОШИБКАХ
            if (!ModelState.IsValid)
            {
                await LoadUserData(userId);
                return Page();
            }

            // 4. СОХРАНЕНИЕ
            user.Username = Input.Username;
            user.Email = Input.Email;

            if (!string.IsNullOrWhiteSpace(Input.NewPassword))
            {
                user.Password = BCrypt.Net.BCrypt.HashPassword(Input.NewPassword);
            }

            if (user.Customer != null)
            {
                user.Customer.FullName = Input.FullName;
                user.Customer.Phone = Input.Phone;
                user.Customer.DefaultAddress = Input.Address;
                user.Customer.Latitude = Input.Latitude;
                user.Customer.Longitude = Input.Longitude;
            }

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        private int GetUserId()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(claim, out int id) ? id : -1;
        }

        private async Task LoadUserData(int userId)
        {
            CurrentUser = await _context.Users
                .Include(u => u.Customer)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (CurrentUser != null)
            {
                Input.Username = CurrentUser.Username;
                Input.Email = CurrentUser.Email;

                if (CurrentUser.Customer != null)
                {
                    Input.FullName = CurrentUser.Customer.FullName;
                    Input.Phone = CurrentUser.Customer.Phone;
                    Input.Address = CurrentUser.Customer.DefaultAddress;
                    Input.Latitude = CurrentUser.Customer.Latitude ?? 53.90;
                    Input.Longitude = CurrentUser.Customer.Longitude ?? 27.56;

                    int targetCustomerId = CurrentUser.Customer.CustomerId;

                    Orders = await _context.CustomerOrders
                        .Where(o => o.CustomerId == targetCustomerId && o.Status != "cart") // Скрываем корзину
                        .OrderByDescending(o => o.CreatedAt)
                        .Select(o => new OrderDisplayViewModel
                        {
                            OrderId = o.OrderId,
                            CreatedAt = o.CreatedAt ?? DateTime.Now,
                            ExpiresAt = o.ExpiresAt, // Дата истечения брони
                            Status = o.Status ?? "новый",
                            TotalCost = o.TotalCost ?? 0m,
                            Items = o.OrderItems.Select(oi => new OrderItemDisplayViewModel
                            {
                                ProductId = oi.Stock.ProductId,
                                ProductName = oi.Stock.Product.Name,
                                Quantity = oi.Quantity,
                                Price = oi.Stock.Price ?? 0m,
                                // Тянем данные аптеки через склад
                                PharmacyName = oi.Stock.Pharmacy.Name ?? "Аптека",
                                PharmacyAddress = oi.Stock.Pharmacy.Address ?? "Адрес не указан"
                            }).ToList()
                        })
                        .ToListAsync();
                }
            }
        }
    }

    public class OrderDisplayViewModel
    {
        public int OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiresAt { get; set; } // Добавили срок
        public string Status { get; set; } = "";
        public decimal TotalCost { get; set; }
        public List<OrderItemDisplayViewModel> Items { get; set; } = new();
    }

    public class OrderItemDisplayViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string PharmacyName { get; set; } = ""; // Название аптеки
        public string PharmacyAddress { get; set; } = ""; // Адрес аптеки
    }
}