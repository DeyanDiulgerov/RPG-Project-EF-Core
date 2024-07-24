using Microsoft.EntityFrameworkCore;
using RPG_Project.CharacterModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RPG_Project.DataConnection
{
    public class RpgDbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            string dataSource = @".\SQLEXPRESS";
            string database = "RPG Project";
            string connString = @"Data Source =" + dataSource + ";Initial Catalog=" + database + ";Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";
            optionsBuilder.UseSqlServer(connString);
        }
        public DbSet<HeroModel> Heroes { get; set; }
        //Works :)  
    }
}


