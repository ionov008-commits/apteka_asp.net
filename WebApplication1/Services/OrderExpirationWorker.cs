using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Services
{
    public class OrderExpirationWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderExpirationWorker> _logger;

        public OrderExpirationWorker(IServiceProvider serviceProvider, ILogger<OrderExpirationWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Фоновая служба проверки просроченных заказов запущена.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<PharmacyContext>();

                        // 1. Ищем активные заказы, у которых истек срок
                        var expiredOrders = await db.CustomerOrders
                            .Include(o => o.OrderItems)
                            .Where(o => o.Status == "active" && o.ExpiresAt < DateTime.Now)
                            .ToListAsync(stoppingToken);

                        if (expiredOrders.Any())
                        {
                            _logger.LogInformation($"Найдено просроченных заказов: {expiredOrders.Count}");

                            foreach (var order in expiredOrders)
                            {
                                // 2. Меняем статус заказа
                                order.Status = "cancelled";

                                // 3. Возвращаем товары обратно на склад!
                                foreach (var item in order.OrderItems)
                                {
                                    var stock = await db.PharmacyStocks.FirstOrDefaultAsync(s => s.StockId == item.StockId, stoppingToken);
                                    if (stock != null)
                                    {
                                        stock.Quantity += item.Quantity; // Плюсуем обратно
                                    }
                                }
                            }

                            // Сохраняем все изменения в БД
                            await db.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при проверке просроченных заказов.");
                }

                // Проверяем базу каждый час (можно поставить и чаще, например TimeSpan.FromMinutes(30))
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}