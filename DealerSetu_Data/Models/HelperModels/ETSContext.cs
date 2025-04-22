using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DealerSetu_Data.Models.HelperModels
{
    public class ETSContext : DbContext
    {
        public ETSContext(DbContextOptions<ETSContext> options) : base(options)
        {
        }

        public DbSet<UserDetails> tb_userDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserDetails>(entity =>
            {
                entity.ToTable("lkpUser");

                entity.Property(e => e.Name)
                      .HasMaxLength(50);
            });
        }
    }
}
