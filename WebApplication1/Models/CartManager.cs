using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Models
{
    public class CartManager
    {
        private readonly PharmacyContext _db;

        public CartManager(PharmacyContext db)
        {
            _db = db;
        }

        // 1. Найти или создать заказ со статусом 'cart'
        public async Task<CustomerOrder> GetOrCreateCartAsync(int userId)
        {
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null) return null;

            var cart = await _db.CustomerOrders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.CustomerId == customer.CustomerId && o.Status == "cart");

            if (cart == null)
            {
                cart = new CustomerOrder
                {
                    CustomerId = customer.CustomerId,
                    Status = "cart",
                    CreatedAt = DateTime.Now,
                    TotalCost = 0
                };
                _db.CustomerOrders.Add(cart);
                await _db.SaveChangesAsync();
            }
            return cart;
        }

        // 2. Обновить количество (финтифлюшка)
        public async Task<(int newQty, decimal totalCost)> UpdateItemAsync(int userId, int stockId, int delta)
        {
            var cart = await GetOrCreateCartAsync(userId);
            var stock = await _db.PharmacyStocks.FindAsync(stockId);

            if (cart == null || stock == null) return (0, 0);

            var item = cart.OrderItems.FirstOrDefault(i => i.StockId == stockId);

            if (item != null)
            {
                item.Quantity += delta;
                // Проверка: не заказываем больше, чем есть физически
                if (item.Quantity > stock.Quantity) item.Quantity = stock.Quantity ?? 0;

                if (item.Quantity <= 0) _db.OrderItems.Remove(item);
            }
            else if (delta > 0)
            {
                item = new OrderItem { OrderId = cart.OrderId, StockId = stockId, Quantity = 1 };
                _db.OrderItems.Add(item);
            }

            await _db.SaveChangesAsync();

            // Считаем общую стоимость корзины
            var total = await _db.OrderItems
                .Where(i => i.OrderId == cart.OrderId)
                .SumAsync(i => i.Quantity * i.Stock.Price);

            cart.TotalCost = total;
            await _db.SaveChangesAsync();

            return (item?.Quantity ?? 0, total ?? 0);
        }
    }
}