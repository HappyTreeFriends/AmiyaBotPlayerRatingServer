using AmiyaBotPlayerRatingServer.Model;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
#pragma warning disable CS8618

namespace AmiyaBotPlayerRatingServer.Data
{
    public class PlayerRatingDatabaseContext : DbContext
    {
        private IConfiguration Configuration { get; }

        public PlayerRatingDatabaseContext(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            base.OnConfiguring(options);

            var host = Configuration["Db:Host"];
            var port = Configuration["Db:Port"];
            var database = Configuration["Db:Database"];
            var username = Configuration["Db:Username"];
            var password = Configuration["Db:Password"];
            var conn =
                $"Host={host};Port={port};Database={database};Username={username};Password={password};Maximum Pool Size=50";

            options.UseNpgsql(conn);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CharacterStatistics>()
                .Property(e => e.AverageSpecializeLevel)
                .HasConversion(
                    v => JsonConvert.SerializeObject(v),
                    v => JsonConvert.DeserializeObject<List<double>>(v));

            modelBuilder.Entity<CharacterStatistics>()
                .Property(e => e.AverageEquipLevel)
                .HasConversion(
                    v => JsonConvert.SerializeObject(v),
                    v => JsonConvert.DeserializeObject<Dictionary<int, double>>(v));
        }


        public DbSet<CharacterStatistics> CharacterStatistics { get; set; }
    }
}
