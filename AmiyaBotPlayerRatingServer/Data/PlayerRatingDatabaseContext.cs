﻿using AmiyaBotPlayerRatingServer.Model;
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

        public static String GetConnectionString(IConfiguration configuration)
        {
            var host = configuration["Db:Host"];
            var port = configuration["Db:Port"];
            var database = configuration["Db:Database"];
            var username = configuration["Db:Username"];
            var password = configuration["Db:Password"];
            var conn =
                $"Host={host};Port={port};Database={database};Username={username};Password={password};Maximum Pool Size=50";
            return conn;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            base.OnConfiguring(options);

            options.UseNpgsql(GetConnectionString(Configuration));
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
