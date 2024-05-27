using AmiyaBotPlayerRatingServer.Model;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Hangfire;
using Hangfire.PostgreSql;
#pragma warning disable CS8618

namespace AmiyaBotPlayerRatingServer.Data
{
    public class PlayerRatingDatabaseContext : IdentityDbContext<ApplicationUser>
    {
        private IConfiguration Configuration { get; }

        public PlayerRatingDatabaseContext(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public static String GetConnectionString(IConfiguration configuration)
        {
            var host = configuration["Db:Host"];
            var port = configuration["Db:Port"];
            var database = configuration["Db:Database"];
            var username = configuration["Db:Username"];
            var password = configuration["Db:Password"];
            var dbType = configuration["Db:Type"]?.ToUpper();
            String conn;
            switch (dbType)
            {
                //case "POSTGRESQL":
                default:
                    conn =
                        $"Host={host};Port={port};Database={database};Username={username};Password={password};Maximum Pool Size=50";
                    break;
            }

            return conn;
        }

        public static JobStorage GetHangfireJobStorage(IConfiguration configuration)
        {
            var dbType = configuration["Db:Type"]?.ToUpper();
            switch (dbType)
            {
                //case "POSTGRESQL":
                default:
#pragma warning disable CS0618
                    return new PostgreSqlStorage(GetConnectionString(configuration));
#pragma warning restore CS0618
            }
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            base.OnConfiguring(options);

            var dbType = Configuration["Db:Type"]?.ToUpper();
            switch (dbType)
            {
                //case "POSTGRESQL":
                default:
                    options.UseNpgsql(GetConnectionString(Configuration));
                    break;
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CharacterStatistics>()
                .Property(e => e.AverageSpecializeLevel)
                .HasConversion(
                    v => JsonConvert.SerializeObject(v),
                    v => JsonConvert.DeserializeObject<List<double>>(v) ?? new List<double>());

            modelBuilder.Entity<CharacterStatistics>()
                .Property(e => e.AverageEquipLevel)
                .HasConversion(
                    v => JsonConvert.SerializeObject(v),
                    v => JsonConvert.DeserializeObject<Dictionary<int, double>>(v) ?? new Dictionary<int, double>());
            
            modelBuilder.Entity<MAATask>().HasMany(e => e.SubTasks)
                .WithOne(e => e.ParentTask)
                .HasForeignKey(e => e.ParentTaskId)
                .OnDelete(DeleteBehavior.Restrict);
            
            modelBuilder.Entity<MAARepetitiveTask>()
                .HasMany(r => r.SubTasks)  
                .WithOne(t => t.ParentRepetitiveTask) 
                .HasForeignKey(t => t.ParentRepetitiveTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GameInfo>()
                .HasMany(g => g.PlayerList)
                .WithMany();

            modelBuilder.UseOpenIddict();
        }


        public DbSet<CharacterStatistics> CharacterStatistics { get; set; }
        public DbSet<SKLandCredential> SKLandCredentials { get; set; }
        public DbSet<SKLandCharacterBox> SKLandCharacterBoxes { get; set; }
        public DbSet<ClientInfo> ClientInfos { get; set; }

        public DbSet<SystemNotification> SystemNotifications { get; set; }

        public DbSet<MAAConnection> MAAConnections { get; set; }
        public DbSet<MAATask> MAATasks { get; set; }
        public DbSet<MAARepetitiveTask> MAARepetitiveTasks { get; set; }
        public DbSet<MAAResponse> MAAResponses { get; set; }

        public DbSet<GameInfo> GameInfos { get; set; }
        public DbSet<ApplicationUserMinigameStatistics> ApplicationUserMinigameStatistics { get; set; }
    }
}
