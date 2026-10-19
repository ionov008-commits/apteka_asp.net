using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;
using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication1.Pages.Pharmacy
{
    // [Authorize(Roles = "pharmacist")]
    public class PharmacistModel : PageModel
    {
        private readonly PharmacyContext _context;

        public PharmacistModel(PharmacyContext context)
        {
            _context = context;
        }

        public CustomerOrder? CurrentOrder { get; set; }
        public List<OrderItem> OrderItems { get; set; } = new();
        public string? ErrorMessage { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? SearchOrderId { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (SearchOrderId.HasValue)
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdString, out int userId))
                {
                    // Временная заглушка
                    userId = _context.Pharmacists.Select(p => p.UserId).FirstOrDefault();
                }

                var pharmacist = await _context.Pharmacists.FirstOrDefaultAsync(p => p.UserId == userId);
                if (pharmacist == null)
                {
                    ErrorMessage = "Фармацевт не найден или не привязан к аптеке.";
                    return Page();
                }

                CurrentOrder = await _context.CustomerOrders
                    .Include(o => o.Customer)
                    .FirstOrDefaultAsync(o => o.OrderId == SearchOrderId.Value);

                if (CurrentOrder == null)
                {
                    ErrorMessage = $"Заказ #{SearchOrderId} не найден.";
                    return Page();
                }

                // Загружаем только позиции НАШЕЙ аптеки
                OrderItems = await _context.OrderItems
                    .Include(oi => oi.Stock)
                        .ThenInclude(s => s.Product)
                    .Where(oi => oi.OrderId == SearchOrderId.Value && oi.Stock.PharmacyId == pharmacist.PharmacyId)
                    .ToListAsync();

                if (!OrderItems.Any())
                {
                    ErrorMessage = "В этом заказе нет товаров из вашей аптеки.";
                    CurrentOrder = null;
                }
            }

            return Page();
        }

        // ВАЖНО: Добавлен список cancelledItemIds, который приходит из чекбоксов
        public async Task<IActionResult> OnPostIssueOrderAsync(int orderId, List<int> cancelledItemIds)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = int.TryParse(userIdString, out int id) ? id : _context.Pharmacists.Select(p => p.UserId).FirstOrDefault();

            var pharmacist = await _context.Pharmacists.FirstOrDefaultAsync(p => p.UserId == userId);
            if (pharmacist == null) return RedirectToPage(new { ErrorMessage = "Ошибка доступа." });

            var order = await _context.CustomerOrders.FindAsync(orderId);
            if (order == null) return RedirectToPage(new { SearchOrderId = orderId });

            var orderItems = await _context.OrderItems
                .Include(oi => oi.Stock)
                .Where(oi => oi.OrderId == orderId && oi.Stock.PharmacyId == pharmacist.PharmacyId)
                .ToListAsync();

            if (!orderItems.Any()) return RedirectToPage(new { SearchOrderId = orderId });

            // 1. Проходим по всем товарам нашей аптеки в этом заказе
            foreach (var item in orderItems)
            {
                // Если фармацевт поставил галочку "Отказ"
                if (cancelledItemIds != null && cancelledItemIds.Contains(item.OrderItemId))
                {
                    item.Status = "Cancelled";
                    // Списывать со склада НЕ нужно
                }
                else
                {
                    // Товар успешно выдан
                    item.Status = "Dispensed";
                    item.Stock.Quantity -= item.Quantity; // Списываем остаток
                    if (item.Stock.Quantity < 0) item.Stock.Quantity = 0;
                }
            }

            // Сохраняем статусы позиций и остатки на складе
            await _context.SaveChangesAsync();

            // 2. Проверяем, остались ли во ВСЕМ заказе невыданные позиции (например, в другой аптеке)
            bool anyPendingLeft = await _context.OrderItems.AnyAsync(oi => oi.OrderId == orderId && oi.Status == "Pending");

            // Если все товары либо выданы, либо отменены — закрываем сам заказ
            if (!anyPendingLeft)
            {
                order.Status = "выдан";
                await _context.SaveChangesAsync();
            }

            return RedirectToPage(new { SearchOrderId = orderId, SuccessMessage = "Заказ успешно обработан! Брони сняты, остатки обновлены." });
        }
    }
}