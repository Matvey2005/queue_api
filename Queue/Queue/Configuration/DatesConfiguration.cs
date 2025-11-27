using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Queue.Models;


namespace YourApp.Data.Configurations
{
    public class DatesConfiguration : IEntityTypeConfiguration<Dates>
    {
        public void Configure(EntityTypeBuilder<Dates> builder)
        {

            builder.HasKey(d => d.Id);

            builder.Property(d => d.Date)
                .IsRequired()
                .HasMaxLength(20);

            builder.Property(d => d.ForSubgroup)
                .IsRequired();

            // 🔗 Связь: один предмет → много дат
            builder.HasOne<Subject>()
                .WithMany()
                .HasForeignKey(d => d.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.GroupId);
        }
    }
}
