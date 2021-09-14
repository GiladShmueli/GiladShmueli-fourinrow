using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameDB_create
{
    public class GameContext: DbContext
    {

        private const string connectionString =
            @"Data Source=(LocalDB)\MSSQLLocalDB;" +
            @"Initial Catalog=fourinrow_gilad_ilya;" +
            @"AttachDbFilename= C:\fourinrow\fourinrow_gilad_ilya.mdf;" +
            @"Integrated Security=True; Connect Timeout=30";

        #region overrides
        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        {
            base.OnConfiguring(builder);
            builder.UseSqlServer(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            
            /*builder.Entity<StudentCourse>().HasOne(sc => sc.Student)
                .WithMany(s => s.StudentCourses)
                .HasForeignKey(sc => sc.StudentId);
            builder.Entity<StudentCourse>().HasOne(sc => sc.Course)
                .WithMany(c => c.StudentCourses)
                .HasForeignKey(sc => sc.CourseId);*/

            builder.Entity<Player>().
                HasMany(p => p.BlueGames).
                WithOne(g => g.BluePlayer);

            builder.Entity<Player>().
                HasMany(p => p.RedGames).
                WithOne(g => g.RedPlayer);

            builder.Entity<Player>().
                HasMany(p => p.Victories).
                WithOne(g => g.Winner);
        }
        #endregion
        public DbSet<Player> Players { get; set; }
        public DbSet<Game> Games { get; set; }
    }
}
