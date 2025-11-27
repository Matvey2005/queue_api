using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Queue.Models;

namespace Queue.Configuration
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {

            builder.HasKey(u => u.Id);

            builder.Property(u => u.UserName)
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(30);

            builder.Property(u => u.Password)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(u => u.SubgroupNumber)
                .IsRequired();

            // 🔗 Одна группа → много пользователей
            builder.HasOne<Group>()
                .WithMany()
                .HasForeignKey(u => u.GroupId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
