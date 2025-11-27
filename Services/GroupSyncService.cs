using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Queue.Repositories;

namespace Queue.Services
{
    public class GroupSyncService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GroupSyncService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromHours(1); // 🔁 Раз в час (можно поменять)

        public GroupSyncService(IServiceProvider serviceProvider, ILogger<GroupSyncService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🟢 GroupSyncService запущен.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var groupRepo = scope.ServiceProvider.GetRequiredService<GroupRepository>();
                        var addedCount = await groupRepo.SaveGroupsToDatabaseAsync();

                        if (addedCount > 0)
                            _logger.LogInformation($"✅ Добавлено {addedCount} новых групп.");
                        else
                            _logger.LogInformation("ℹ️ Новых групп нет.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Ошибка при синхронизации групп");
                }

                // ⏰ Ждём до следующей проверки
                await Task.Delay(_interval, stoppingToken);
            }

            _logger.LogInformation("🛑 GroupSyncService остановлен.");
        }
    }
}
