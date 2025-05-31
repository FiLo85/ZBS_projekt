using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using WeatherChecker_FO.Models;

namespace WeatherChecker_FO.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Account> Accounts { get; set; }
    }
}
