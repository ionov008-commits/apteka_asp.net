using System;
using System.Collections.Generic;
using System.Linq; // Не забудь добавить этот using для .Where и .Sum

namespace WebApplication1.Models;

public partial class PharmacyStock
{
    public int StockId { get; set; }

    public int PharmacyId { get; set; }

    public int ProductId { get; set; }

    public int? Quantity { get; set; }

    public decimal? Price { get; set; }

    public virtual Pharmacy Pharmacy { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;

    // Оставляем только ОДНО определение
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    // Вычисляемое свойство
    public int AvailableQuantity
    {
        get
        {
            int total = Quantity ?? 0;

            // Считаем резерв
            // ВАЖНО: убедись, что при запросе в БД ты делаешь .Include(s => s.OrderItems).ThenInclude(oi => oi.Order)
            int reserved = OrderItems?
                .Where(oi => oi.Order != null && (oi.Order.Status == "booked" || oi.Order.Status == "active"))
                .Sum(oi => oi.Quantity) ?? 0;

            return total - reserved;
        }
    }
}