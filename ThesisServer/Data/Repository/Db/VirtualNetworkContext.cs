using Microsoft.EntityFrameworkCore;

namespace ThesisServer.Data.Repository.Db
{
    public class VirtualNetworkDbContext : DbContext
    {
        public VirtualNetworkDbContext(DbContextOptions<VirtualNetworkDbContext> options) : base(options) { }

        public DbSet<UserEntity> User { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            UserEntityModelCreating(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        private void UserEntityModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserEntity>().HasKey(x => x.Token1);
            modelBuilder.Entity<UserEntity>().HasIndex(x => x.Token2);
        }
    }
}
