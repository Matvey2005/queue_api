using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Queue.Models;

namespace Queue.Configuration
{
    public class ExchangeConfiguration : IEntityTypeConfiguration<Exchange>
    {
        public void Configure(EntityTypeBuilder<Exchange> builder)
        {
            builder.HasKey(e => e.Id); // Установка первичного ключа

            builder.HasOne(e => e.FromUser) // Настройка связи с FromUser
                .WithMany() // Сообщает, что у User может быть много Exchange
                .HasForeignKey(e => e.FromUserId) // Определяем внешний ключ
                .OnDelete(DeleteBehavior.Restrict); // Указываем поведение при удалении

            builder.HasOne(e => e.ToUser) // Настройка связи с ToUser
                .WithMany() // Сообщает, что у User может быть много Exchange
                .HasForeignKey(e => e.ToUserId) // Определяем внешний ключ
                .OnDelete(DeleteBehavior.Restrict); // Указываем поведение при удалении

            builder.HasOne(e => e.DateNavigation) // Настройка связи с Date
            .WithMany(d => d.Exchanges) // Сообщает, что у Dates может быть много Exchange
            .HasForeignKey(e => e.DateId) // Определяем внешний ключ
            .OnDelete(DeleteBehavior.Restrict); // Указываем поведение при удалении
        }
    }
}
