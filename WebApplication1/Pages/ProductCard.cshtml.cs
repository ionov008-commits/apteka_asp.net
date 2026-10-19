using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApplication1.Models;

namespace WebApplication1.Pages
{
    public class ProductCardModel : PageModel
    {
        private readonly PharmacyContext _context;

        public ProductCardModel(PharmacyContext context)
        {
            _context = context;
        }

        public Product Product { get; set; }
        public List<PharmacyOfferViewModel> Offers { get; set; } = new();

        public string UserAddress { get; set; } = "г. Минск";
        public double? UserLat { get; set; }
        public double? UserLon { get; set; }

        public class PharmacyOfferViewModel
        {
            public int PharmacyId { get; set; }
            public string Name { get; set; }
            public string Address { get; set; }
            public decimal Price { get; set; }
            public string Phone { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public int Quantity { get; set; }

            // Сколько этого товара уже лежит в корзине
            public int CartQuantity { get; set; }
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            // Загружаем основной продукт
            Product = await _context.Products.FirstOrDefaultAsync(m => m.ProductId == id);
            if (Product == null) return NotFound();

            // Определяем текущего пользователя
            int userId = 0;
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int.TryParse(userIdString, out userId);

            // Подгружаем данные профиля (адрес и координаты)
            if (userId != 0)
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
                if (customer != null)
                {
                    if (!string.IsNullOrWhiteSpace(customer.DefaultAddress))
                        UserAddress = customer.DefaultAddress;
                    UserLat = customer.Latitude;
                    UserLon = customer.Longitude;
                }
            }

            // ШАГ 1: Загружаем корзину ТЕКУЩЕГО пользователя
            List<OrderItem> currentCartItems = new List<OrderItem>();
            if (userId != 0)
            {
                var cart = await _context.CustomerOrders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Customer.UserId == userId && o.Status == "cart");

                if (cart != null)
                {
                    currentCartItems = cart.OrderItems.ToList();
                }
            }

            // ШАГ 2: Загружаем предложения аптек
            // ВАЖНО: используем Include для OrderItems и Order, чтобы AvailableQuantity сработал корректно
            var stockItems = await _context.PharmacyStocks
                .Include(s => s.Pharmacy)
                .Include(s => s.OrderItems)
                    .ThenInclude(oi => oi.Order)
                .Where(s => s.ProductId == id && s.Quantity > 0)
                .ToListAsync();

            // Очищаем старые предложения перед заполнением
            Offers.Clear();

            foreach (var item in stockItems)
            {
                // Используем твою новую переменную из модели PharmacyStock
                // Она сама вычтет "booked" и "active" из общего Quantity
                int available = item.AvailableQuantity;

                // Если все лекарства забронированы другими, юзеру это предложение не показываем
                if (available <= 0) continue;

                // Проверяем, сколько конкретно этого товара уже лежит в корзине у текущего юзера
                var itemInCart = currentCartItems.FirstOrDefault(i => i.StockId == item.StockId);
                int currentQtyInCart = itemInCart != null ? itemInCart.Quantity : 0;

                Offers.Add(new PharmacyOfferViewModel
                {
                    PharmacyId = item.PharmacyId,
                    Name = item.Pharmacy.Name ?? "Аптека",
                    Address = item.Pharmacy.Address ?? "",
                    Price = item.Price ?? 0m,
                    Phone = item.Pharmacy.Phone ?? "",
                    Quantity = available, // Показываем чистый остаток (напр. 5 вместо 15)
                    Latitude = item.Pharmacy.Latitude ?? 0,
                    Longitude = item.Pharmacy.Longitude ?? 0,
                    CartQuantity = currentQtyInCart
                });
            }

            // Сортируем по цене (самые дешевые сверху)
            Offers = Offers.OrderBy(o => o.Price).ToList();

            return Page();
        }

        // Изменение количества товара в корзине
        public async Task<IActionResult> OnPostAddToCartAsync(int productId, int pharmacyId, int quantity)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !User.IsInRole("customer"))
                return RedirectToPage("/Account/Login");

            int userId = int.Parse(userIdClaim);

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null) return NotFound("Профиль покупателя не найден");

            var order = await _context.CustomerOrders
                .FirstOrDefaultAsync(o => o.CustomerId == customer.CustomerId && o.Status == "cart");

            if (order == null)
            {
                order = new CustomerOrder { CustomerId = customer.CustomerId, Status = "cart", CreatedAt = DateTime.Now, TotalCost = 0 };
                _context.CustomerOrders.Add(order);
                await _context.SaveChangesAsync();
            }

            var stock = await _context.PharmacyStocks
                .FirstOrDefaultAsync(s => s.ProductId == productId && s.PharmacyId == pharmacyId);

            if (stock == null) return RedirectToPage(new { id = productId });

            var existingItem = await _context.OrderItems
                .FirstOrDefaultAsync(i => i.OrderId == order.OrderId && i.StockId == stock.StockId);

            // Если запросили 0 или меньше - удаляем из корзины
            if (quantity <= 0)
            {
                if (existingItem != null)
                {
                    _context.OrderItems.Remove(existingItem);
                }
            }
            // Иначе устанавливаем точное количество
            else
            {
                int finalQuantity = quantity > stock.Quantity.Value ? stock.Quantity.Value : quantity;

                if (existingItem != null)
                {
                    existingItem.Quantity = finalQuantity;
                }
                else
                {
                    var newItem = new OrderItem { OrderId = order.OrderId, StockId = stock.StockId, Quantity = finalQuantity };
                    _context.OrderItems.Add(newItem);
                }
            }

            await _context.SaveChangesAsync();

            // Пересчет итоговой стоимости
            var items = await _context.OrderItems
                .Where(i => i.OrderId == order.OrderId)
                .Select(i => new { i.Quantity, Price = i.Stock.Price ?? 0m })
                .ToListAsync();

            order.TotalCost = items.Sum(i => i.Quantity * i.Price);
            await _context.SaveChangesAsync();

            return RedirectToPage("", "", new { id = productId }, "offersTable");
        }

        // НОВЫЙ МЕТОД: Логика подтверждения бронирования (оформление заказа)
        public async Task<IActionResult> OnPostConfirmBookingAsync(int productId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return RedirectToPage("/Account/Login");

            int userId = int.Parse(userIdClaim);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null) return NotFound("Профиль покупателя не найден");

            // Ищем текущую корзину со всеми позициями
            var order = await _context.CustomerOrders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Stock)
                .FirstOrDefaultAsync(o => o.CustomerId == customer.CustomerId && o.Status == "cart");

            if (order == null || !order.OrderItems.Any())
            {
                // Если корзина пуста, просто обновляем страницу
                return RedirectToPage(new { id = productId });
            }

            // Проверка 1: Не раскупили ли товар, пока пользователь думал
            foreach (var item in order.OrderItems)
            {
                if (item.Quantity > item.Stock.Quantity)
                {
                    ModelState.AddModelError(string.Empty, $"К сожалению, аптека больше не располагает нужным количеством товара. Доступно: {item.Stock.Quantity}.");

                    // Перезагружаем данные для отображения страницы с ошибкой
                    await OnGetAsync(productId);
                    return Page();
                }
            }

            // Проверка 2: Списание количества со склада при бронировании
            foreach (var item in order.OrderItems)
            {
                item.Stock.Quantity -= item.Quantity;
            }

            // Переводим заказ из статуса корзины в бронь
            order.Status = "booked"; // или "pending" / "confirmed" в зависимости от вашей БД
            order.CreatedAt = DateTime.Now; // Обновляем дату на момент бронирования

            await _context.SaveChangesAsync();

            // Безопасный редирект на профиль. 
            // Если используется ASP.NET Core Identity и страница в области (Area), 
            // может потребоваться: return RedirectToPage("/Account/Profile", new { area = "Identity" });
            return RedirectToPage("/Account/Profile");
        }
    }
}