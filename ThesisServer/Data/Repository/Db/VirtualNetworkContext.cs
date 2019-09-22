using Microsoft.EntityFrameworkCore;

namespace ThesisServer.Data.Repository.Db
{
    public class VirtualNetworkDbContext : DbContext
    {
        public VirtualNetworkDbContext(DbContextOptions<VirtualNetworkDbContext> options) : base(options) { }

        public DbSet<UserEntity> User { get; set; }
        public DbSet<NetworkEntity> Network { get; set; }
        public DbSet<VirtualFileEntity> VirtualFile { get; set; }
        public DbSet<VirtualFilePieceEntity> VirtualFilePiece { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            UserEntityModelCreating(modelBuilder);

            NetworkEntityModelCreating(modelBuilder);

            VirtualFileEntityModelCreating(modelBuilder);

            VirtualFilePieceModelCreating(modelBuilder);

            base.OnModelCreating(modelBuilder);
        }

        private void VirtualFilePieceModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VirtualFilePieceEntity>().HasKey(x => x.FilePieceId);

            modelBuilder.Entity<VirtualFilePieceEntity>()
                .HasOne(x => x.File)
                .WithMany(x => x.FilePieces)
                .HasForeignKey(x => x.FileId)
                .HasConstraintName("ForeignKey_VirtualFilePieceEntity_VirtualFileEntity");
        }

        private void VirtualFileEntityModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<VirtualFileEntity>().HasKey(x => x.FileId);
        }

        private void NetworkEntityModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<NetworkEntity>().HasKey(x => x.NetworkId);
            //modelBuilder.Entity<NetworkEntity>().Property(x => x.NetworkId).UseSqlServerIdentityColumn();
        }

        private void UserEntityModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserEntity>().HasKey(x => x.Token1);
            modelBuilder.Entity<UserEntity>().HasIndex(x => x.Token2);
            modelBuilder.Entity<UserEntity>().Property(x => x.MaxSpace).HasDefaultValue(300);

            modelBuilder
                .Entity<UserEntity>()
                .HasOne(x => x.Network)
                .WithMany(x => x.Users)
                .HasForeignKey(x => x.NetworkId)
                .HasConstraintName("ForeignKey_UserEntity_NetworkEntity");
        }
    }
}
