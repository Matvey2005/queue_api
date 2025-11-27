using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Queue.Configuration
{
    public class QueueConfiguration : IEntityTypeConfiguration<Models.Queue> // 👈 важно!
    {
        public void Configure(EntityTypeBuilder<Models.Queue> builder)
        {

            builder.HasKey(q => q.Id);

            builder.Property(q => q.NumberUser)
                .IsRequired();

            builder.HasOne<Models.User>()
                .WithMany()
                .HasForeignKey(q => q.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<Models.Dates>()
                .WithMany()
                .HasForeignKey(q => q.DateId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
