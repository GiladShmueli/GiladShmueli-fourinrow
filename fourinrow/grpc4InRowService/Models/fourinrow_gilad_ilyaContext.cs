using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace grpc4InRowService.Models
{
    public partial class fourinrow_gilad_ilyaContext : DbContext
    {
        public fourinrow_gilad_ilyaContext()
        {
        }

        public fourinrow_gilad_ilyaContext(DbContextOptions<fourinrow_gilad_ilyaContext> options)
            : base(options)
        {
        }

        public virtual DbSet<Game> Games { get; set; }
        public virtual DbSet<Player> Players { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseSqlServer("Data Source=(LocalDB)\\MSSQLLocalDB; Initial Catalog=fourinrow_gilad_ilya;AttachDbFilename= C:\\fourinrow\\fourinrow_gilad_ilya.mdf;Integrated Security=True;Connect Timeout=120;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS");

            modelBuilder.Entity<Game>(entity =>
            {
                entity.HasIndex(e => e.BluePlayerUserId, "IX_Games_BluePlayerUserId");

                entity.HasIndex(e => e.RedPlayerUserId, "IX_Games_RedPlayerUserId");

                entity.HasIndex(e => e.WinnerUserId, "IX_Games_WinnerUserId");

                entity.HasOne(d => d.BluePlayerUser)
                    .WithMany(p => p.GameBluePlayerUsers)
                    .HasForeignKey(d => d.BluePlayerUserId);

                entity.HasOne(d => d.RedPlayerUser)
                    .WithMany(p => p.GameRedPlayerUsers)
                    .HasForeignKey(d => d.RedPlayerUserId);

                entity.HasOne(d => d.WinnerUser)
                    .WithMany(p => p.GameWinnerUsers)
                    .HasForeignKey(d => d.WinnerUserId);
            });

            modelBuilder.Entity<Player>(entity =>
            {
                entity.HasKey(e => e.UserId);

                entity.Property(e => e.UserId).ValueGeneratedNever();
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
