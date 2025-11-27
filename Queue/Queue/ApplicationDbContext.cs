using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Queue.Configuration;
using Queue.Models;
using YourApp.Data.Configurations;

namespace Queue
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Dates> Dates { get; set; }
        public DbSet<Models.Queue> Queue { get; set; }
        public DbSet<Exchange> Exchanges { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new GroupConfiguration());
            modelBuilder.ApplyConfiguration(new SubjectConfiguration());
            modelBuilder.ApplyConfiguration(new DatesConfiguration());
            modelBuilder.ApplyConfiguration(new QueueConfiguration());
            modelBuilder.ApplyConfiguration(new ExchangeConfiguration());
            base.OnModelCreating(modelBuilder);
        }
    }
}
