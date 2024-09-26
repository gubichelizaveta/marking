using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marking_TZ.Models
{
    public class AppDBContext : DbContext
    {

        public DbSet<Bottle> Bottles { get; set; }
        public DbSet<Box> Boxes { get; set; }
        public DbSet<Pallet> Pallets { get; set; }
        protected override void OnConfiguring(
            DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite(
                "Data Source=products.db");
        }
        public void Initialize()
        {
            Database.EnsureCreated();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Bottle>()
                .HasOne(b => b.Box)
                .WithMany()
                .HasForeignKey(b => b.BoxId);

            modelBuilder.Entity<Box>()
                .HasOne(b => b.Pallet)
                .WithMany()
                .HasForeignKey(b => b.PalletId);
            modelBuilder.Entity<Pallet>()
      .HasMany(p => p.Boxes)
      .WithOne(b => b.Pallet)
      .HasForeignKey(b => b.PalletId);

            modelBuilder.Entity<Box>()
                .HasMany(b => b.Bottles)
                .WithOne(bt => bt.Box)
                .HasForeignKey(bt => bt.BoxId);
        }

    }
}
