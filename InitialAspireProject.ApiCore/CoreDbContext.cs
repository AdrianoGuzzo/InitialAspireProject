using InitialAspireProject.ApiCore.Entities;
using Microsoft.EntityFrameworkCore;

namespace InitialAspireProject.ApiCore
{
    public class CoreDbContext : DbContext
    {
        public CoreDbContext(DbContextOptions<CoreDbContext> options) : base(options)
        {
        }

        public DbSet<WeatherForecastEntity> WeatherForecast { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);            
        }
    }
}
